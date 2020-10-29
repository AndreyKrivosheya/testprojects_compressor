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
                    this.A = this.Previous.A + 1;
                }
                else
                {
                    this.A = 0;
                }
            }
            public AwaiterForAddedToQueue()
                : this(null)
            {
            }

            readonly int A;

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

            readonly Common.Processors ProcessorsWaitAllPreviousBlocksProcessedAddedToQueueToWrite = new Common.Processors();

            public IAsyncResult BeginWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
            {
                lock(ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite)
                {
                    var previous = Previous;
                    if(previous == null)
                    {
                        return ProcessorsWaitAllPreviousBlocksProcessedAddedToQueueToWrite.BeginRunCompleted(asyncCallback, state);
                    }
                    else
                    {
                        var asyncResultToBeCompleted = ProcessorsWaitAllPreviousBlocksProcessedAddedToQueueToWrite.BeginRunToBeCompleted(asyncCallback, state);
                        // spin off wait for previsous is notified added
                        previous.BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(cancellationToken, asyncCallback:
                            (ar) => {
                                previous.EndWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(ar);
                                ((Common.AsyncResult)asyncResultToBeCompleted).SetAsCompleted(false);
                            });

                        return asyncResultToBeCompleted;
                    }
                }
            }
            public IAsyncResult BeginWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(AsyncCallback asyncCallback = null, object state = null)
            {
                return BeginWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(CancellationToken.None, asyncCallback, state);
            }

            public void EndWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(IAsyncResult waitingAsyncResult)
            {
                ProcessorsWaitAllPreviousBlocksProcessedAddedToQueueToWrite.EndRun(waitingAsyncResult,
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

            readonly Common.Processors ProcessorsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite = new Common.Processors();

            public IAsyncResult BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
            {
                lock(ProcessorsWaitThisBlockProcessedAndAddedToQueueToWrite)
                {
                    var previous = Previous;
                    if(previous == null)
                    {
                        var asyncResultToBeCompleted = ProcessorsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite.BeginRunToBeCompleted(asyncCallback, state);
                        // spin off waiting for this to be notified added
                        this.BeginWaitThisBlockProcessedAndAddedToQueueToWrite(cancellationToken, (ar) => {
                            this.EndWaitThisBlockProcessedAndAddedToQueueToWrite(ar);
                            ((Common.AsyncResult)asyncResultToBeCompleted).SetAsCompleted(false);
                        });

                        return asyncResultToBeCompleted;
                    }
                    else
                    {
                        var asyncResultToBeCompleted = ProcessorsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite.BeginRunToBeCompleted(asyncCallback, state);
                        // spin off waiting for all previous to be notified added
                        previous.BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(cancellationToken, (ar) => {
                            previous.EndWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(ar);
                            // spin off waiting for this to be notified added
                            this.BeginWaitThisBlockProcessedAndAddedToQueueToWrite(cancellationToken, (ar) => {
                                this.EndWaitThisBlockProcessedAndAddedToQueueToWrite(ar);
                                ((Common.AsyncResult)asyncResultToBeCompleted).SetAsCompleted(false);
                            });
                        });

                        return asyncResultToBeCompleted;
                    }
                }
            }
            public IAsyncResult BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(AsyncCallback asyncCallback = null, object state = null)
            {
                return BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(CancellationToken.None, asyncCallback, state);
            }

            public void EndWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(IAsyncResult waitingAsyncResult)
            {
                ProcessorsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite.EndRun(waitingAsyncResult,
                    onAsyncResultNotFromThisProcessors: () => {
                        throw new InvalidOperationException("End of asynchronius wait request did not originate from a BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite() method on this block");
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