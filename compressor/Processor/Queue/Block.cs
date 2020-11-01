using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Processor.Queue
{
    abstract class Block
    {
        protected class AwaiterForAddedToQueue
        {
            public AwaiterForAddedToQueue(AwaiterForAddedToQueue previous)
            {
                if(previous != null)
                {
                    this.Previous = previous;
                    this.Previous.Last = false;
                    //this.A = this.Previous.A + 1;
                }
                else
                {
                    //this.A = 0;
                }
            }
            public AwaiterForAddedToQueue()
                : this(null)
            {
            }

            //readonly int A;

            volatile AwaiterForAddedToQueue Previous = null;

            public bool Last { get; private set; } = true;

            // amount of threads currently trying to begin awaiting for added to queue to collection
            protected volatile int CurrentBeginersForAwaitingForAddedToQueueCount = 0;
            // mask to indicate that added to queue
            protected readonly int CurrentBeginersForAwaitingForAddedToQueueCountAddedToQueueMask = unchecked((int)0x80000000);

            public void NotifyProcessedAndAddedToQueueToWrite()
            {
                var awaiter = new SpinWait();
                while(true)
                {
                    var observedCurrentBeginersForAwaitingForAddedToQueueCount = CurrentBeginersForAwaitingForAddedToQueueCount;
                    // if concurrent NotifyProcessedAndAddedToQueueToWrite() is in progress waiting for all beginers to await to finish
                    if((observedCurrentBeginersForAwaitingForAddedToQueueCount & CurrentBeginersForAwaitingForAddedToQueueCountAddedToQueueMask) == CurrentBeginersForAwaitingForAddedToQueueCountAddedToQueueMask)
                    {
                        // ... then wait all beginers are finished
                        awaiter.Reset();
                        while((CurrentBeginersForAwaitingForAddedToQueueCount & ~CurrentBeginersForAwaitingForAddedToQueueCountAddedToQueueMask) != 0)
                        {
                            awaiter.SpinOnce();
                        }
                        return;
                    }
                    // if this NotifyProcessedAndAddedToQueueToWrite() actualy notified added
                    if(Interlocked.CompareExchange(ref CurrentBeginersForAwaitingForAddedToQueueCount, observedCurrentBeginersForAwaitingForAddedToQueueCount | CurrentBeginersForAwaitingForAddedToQueueCountAddedToQueueMask, observedCurrentBeginersForAwaitingForAddedToQueueCount) == observedCurrentBeginersForAwaitingForAddedToQueueCount)
                    {
                        // ... then spin until all beginers are finished
                        awaiter.Reset();
                        while((CurrentBeginersForAwaitingForAddedToQueueCount & ~CurrentBeginersForAwaitingForAddedToQueueCountAddedToQueueMask) != 0)
                        {
                            awaiter.SpinOnce();
                        }

                        Previous = null;
                        ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite.SetAllToBeCompletedWithoutProcessorAsCompleted(false);
                        return;
                    }
                    awaiter.SpinOnce();
                }
            }

            readonly Common.Processors ProcessorsWaitPreviousBlockProcessedAndAddedToQueueToWrite = new Common.Processors();

            public IAsyncResult BeginWaitPreviousBlockProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
            {
                var previous = Previous;
                if(previous == null)
                {
                    return ProcessorsWaitPreviousBlockProcessedAndAddedToQueueToWrite.BeginRunCompleted(asyncCallback, state);
                }
                else
                {
                    var asyncResultToBeCompleted = ProcessorsWaitPreviousBlockProcessedAndAddedToQueueToWrite.BeginRunToBeCompleted(asyncCallback, state);
                    // spin off wait for previsous is notified added
                    previous.BeginWaitThisBlockProcessedAndAddedToQueueToWrite(cancellationToken, asyncCallback:
                        (ar) => {
                            previous.EndWaitThisBlockProcessedAndAddedToQueueToWrite(ar);
                            ((Common.AsyncResult)asyncResultToBeCompleted).SetAsCompleted(false);
                        });
                    
                    return asyncResultToBeCompleted;
                }
            }
            public IAsyncResult BeginWaitPreviousBlockProcessedAndAddedToQueueToWrite(AsyncCallback asyncCallback = null, object state = null)
            {
                return BeginWaitPreviousBlockProcessedAndAddedToQueueToWrite(CancellationToken.None, asyncCallback, state);
            }

            public void EndWaitPreviousBlockProcessedAndAddedToQueueToWrite(IAsyncResult waitingAsyncResult)
            {
                ProcessorsWaitPreviousBlockProcessedAndAddedToQueueToWrite.EndRun(waitingAsyncResult,
                    onAsyncResultNotFromThisProcessors: () => {
                        throw new InvalidOperationException("End of asynchronius wait request did not originate from a BeginWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite() method on this block");
                    }
                );
            }

            readonly Common.Processors ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite = new Common.Processors();
            
            public IAsyncResult BeginWaitThisBlockProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
            {
                var awaiter = new SpinWait();
                while(true)
                {
                    var observedCurrentBeginersForAwaitingForAddedToQueueCount = CurrentBeginersForAwaitingForAddedToQueueCount;
                    // if already notified added to queue
                    if((observedCurrentBeginersForAwaitingForAddedToQueueCount & CurrentBeginersForAwaitingForAddedToQueueCountAddedToQueueMask) != 0)
                    {
                        return ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite.BeginRunCompleted(asyncCallback, state);
                    }
                    // if this BeginWaitThisBlockProcessedAndAddedToQueueToWrite actually begins wait
                    if(Interlocked.CompareExchange(ref CurrentBeginersForAwaitingForAddedToQueueCount, observedCurrentBeginersForAwaitingForAddedToQueueCount + 1, observedCurrentBeginersForAwaitingForAddedToQueueCount) == observedCurrentBeginersForAwaitingForAddedToQueueCount)
                    {
                        try
                        {
                            if(observedCurrentBeginersForAwaitingForAddedToQueueCount + 1 == ~CurrentBeginersForAwaitingForAddedToQueueCount)
                            {
                                throw new NotSupportedException("Concurrent beginers for awating to be added to queue amount exceeded");
                            }

                            return ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite.BeginRunToBeCompleted(asyncCallback, state);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref CurrentBeginersForAwaitingForAddedToQueueCount);
                        }
                    }
                    awaiter.SpinOnce();
                }
            }
            public IAsyncResult BeginWaitThisBlockProcessedAndAddedToQueueToWrite(AsyncCallback asyncCallback = null, object state = null)
            {
                return BeginWaitThisBlockProcessedAndAddedToQueueToWrite(CancellationToken.None, asyncCallback, state);
            }

            public void EndWaitThisBlockProcessedAndAddedToQueueToWrite(IAsyncResult waitingAsyncResult)
            {
                ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite.EndRun(waitingAsyncResult,
                    onAsyncResultNotFromThisProcessors: () => {
                        throw new InvalidOperationException("End of asynchronius wait request did not originate from a BeginWaitThisBlockProcessedAndAddedToQueueToWrite() method on this block");
                    }
                );
            }
        };

        protected Block(AwaiterForAddedToQueue awaiter, long originalLength, byte[] data)
        {
            this.Awaiter = awaiter;
            this.OriginalLength = originalLength;
            this.Data = data;
        }
        protected Block(Block awaiterBlock, long originalLength, byte[] data)
            : this(awaiterBlock.Awaiter, originalLength, data)
        {
        }

        protected readonly AwaiterForAddedToQueue Awaiter;
        public readonly byte[] Data;
        public long OriginalLength;
    }
}