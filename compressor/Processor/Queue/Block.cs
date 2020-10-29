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

            AwaiterForAddedToQueue Previous = null;

            public bool Last { get; private set; } = true;

            bool ProcessedAndAddedToQueue = false;

            public void NotifyProcessedAndAddedToQueueToWrite()
            {
                Common.Processors ProcessorsWaitThisBlockProcessedAndAddedToQueueToWriteCopy;
                lock(ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite)
                {
                    Previous = null;
                    ProcessedAndAddedToQueue = true;
                    ProcessorsWaitThisBlockProcessedAndAddedToQueueToWriteCopy = new Common.Processors(ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite);
                }
                // notify each of the async awaiters
                ProcessorsWaitThisBlockProcessedAndAddedToQueueToWriteCopy.SetAllToBeCompletedWithoutProcessorAsCompleted(false);
            }

            readonly Common.Processors ProcessorsWaitPreviousBlockProcessedAndAddedToQueueToWrite = new Common.Processors();

            public IAsyncResult BeginWaitPreviousBlockProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
            {
                lock(ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite)
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
                lock(ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite)
                {
                    if(ProcessedAndAddedToQueue)
                    {
                        return ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite.BeginRunCompleted(asyncCallback, state);
                    }
                    else
                    {
                        return ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite.BeginRunToBeCompleted(asyncCallback, state);
                    }
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