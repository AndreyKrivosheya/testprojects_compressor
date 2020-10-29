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
                this.Previous = previous;
                if(this.Previous != null)
                {
                    this.A = this.Previous.A + 1;
                    this.Previous.Last = false;
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
                Dictionary<IAsyncResult, object> IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWriteCopy;
                lock(this)
                {
                    Previous = null;
                    ProcessedAndAddedToQueue = true;
                    IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWriteCopy = new Dictionary<IAsyncResult, object>(IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWrite);
                }
                // notify each of the async awaiters
                foreach(var pair in IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWriteCopy)
                {
                    var asyncResult = ((Common.AsyncResultNoResult)pair.Key);
                    if(!asyncResult.IsCompleted)
                    {
                        asyncResult.SetAsCompleted(false);
                    }
                }
            }

            readonly Dictionary<IAsyncResult, object> IAsyncResultsWaitAllPreviousBlocksProcessedAddedToQueueToWrite = new Dictionary<IAsyncResult, object>();

            public IAsyncResult BeginWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
            {
                lock(this)
                {
                    var previous = Previous;
                    if(previous == null)
                    {
                        var asyncResultCompleted = new Common.AsyncResultNoResult(asyncCallback, state);
                        // add to async results dictionary as soon as possible
                        IAsyncResultsWaitAllPreviousBlocksProcessedAddedToQueueToWrite.Add(asyncResultCompleted, null);
                        // set completed
                        asyncResultCompleted.SetAsCompleted(true);

                        return asyncResultCompleted;
                    }
                    else
                    {
                        var asyncResultToBeCompleted = new Common.AsyncResultNoResult(asyncCallback, state);
                        // add to processors as soon as possible
                        IAsyncResultsWaitAllPreviousBlocksProcessedAddedToQueueToWrite.Add(asyncResultToBeCompleted, null);
                        // spin off wait for previsous is notified added
                        previous.BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(cancellationToken, asyncCallback:
                            (ar) => {
                                previous.EndWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(ar);
                                asyncResultToBeCompleted.SetAsCompleted(false);
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
                lock(this)
                {
                    if(!IAsyncResultsWaitAllPreviousBlocksProcessedAddedToQueueToWrite.ContainsKey(waitingAsyncResult))
                    {
                        throw new InvalidOperationException("End of asynchronius wait request did not originate from a BeginWaitAllPreviousBlocksProcessedAddedToQueueToWrite() method on the current block");
                    }
                }

                try
                {
                    var addingAsyncResultAsCommonAsyncResultNoResult = waitingAsyncResult as Common.AsyncResultNoResult;
                    if(addingAsyncResultAsCommonAsyncResultNoResult != null)
                    {
                        addingAsyncResultAsCommonAsyncResultNoResult.EndInvoke();
                    }
                    else
                    {
                        throw new NotSupportedException("Could not end unexpected but recognized async adding result");
                    }
                }
                finally
                {
                    lock(this)
                    {
                        IAsyncResultsWaitAllPreviousBlocksProcessedAddedToQueueToWrite.Remove(waitingAsyncResult);
                    }
                }
            }

            readonly Dictionary<IAsyncResult, object> IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWrite = new Dictionary<IAsyncResult, object>();
            
            public IAsyncResult BeginWaitThisBlockProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
            {
                lock(this)
                {
                    if(ProcessedAndAddedToQueue)
                    {
                        var asyncResultCompleted = new Common.AsyncResultNoResult(asyncCallback, state);
                        // add to async results dictionary as soon as possible
                        IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWrite.Add(asyncResultCompleted, null);
                        // set completed
                        asyncResultCompleted.SetAsCompleted(true);

                        return asyncResultCompleted;
                    }
                    else
                    {
                        var asyncResultToBeCompleted = new Common.AsyncResultNoResult(asyncCallback, state);
                        // add to processors as soon as possible
                        IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWrite.Add(asyncResultToBeCompleted, null);
                        
                        return asyncResultToBeCompleted;
                    }
                }
            }
            public IAsyncResult BeginWaitThisBlockProcessedAndAddedToQueueToWrite(AsyncCallback asyncCallback = null, object state = null)
            {
                return BeginWaitThisBlockProcessedAndAddedToQueueToWrite(CancellationToken.None, asyncCallback, state);
            }

            public void EndWaitThisBlockProcessedAndAddedToQueueToWrite(IAsyncResult waitingAsyncResult)
            {
                lock(this)
                {
                    if(!IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWrite.ContainsKey(waitingAsyncResult))
                    {
                        throw new InvalidOperationException("End of asynchronius wait request did not originate from a BeginWaitAllPreviousBlocksProcessedAddedToQueueToWrite() method on the current block");
                    }
                }

                try
                {
                    var waitingAsyncResultAsCommonAsyncResultNoResult = waitingAsyncResult as Common.AsyncResultNoResult;
                    if(waitingAsyncResultAsCommonAsyncResultNoResult != null)
                    {
                        waitingAsyncResultAsCommonAsyncResultNoResult.EndInvoke();
                    }
                    else
                    {
                        throw new NotSupportedException("Could not end unexpected but recognized async adding result");
                    }
                }
                finally
                {
                    lock(this)
                    {
                        IAsyncResultsWaitThisBlockProcessedAndAddedToQueueToWrite.Remove(waitingAsyncResult);
                    }
                }
            }

            readonly Dictionary<IAsyncResult, object> IAsyncResultsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite = new Dictionary<IAsyncResult, object>();
            public IAsyncResult BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
            {
                lock(this)
                {
                    var previous = Previous;
                    if(previous == null)
                    {
                        var asyncResultToBeCompleted = new Common.AsyncResultNoResult(asyncCallback, state);
                        // add to async results to be completed
                        IAsyncResultsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite.Add(asyncResultToBeCompleted, null);
                        // spin off waiting for this to be notified added
                        this.BeginWaitThisBlockProcessedAndAddedToQueueToWrite(cancellationToken, (ar) => {
                            this.EndWaitThisBlockProcessedAndAddedToQueueToWrite(ar);
                            asyncResultToBeCompleted.SetAsCompleted(false);
                        });

                        return asyncResultToBeCompleted;
                    }
                    else
                    {
                        var asyncResultToBeCompleted = new Common.AsyncResultNoResult(asyncCallback, state);
                        // add to async results to be completed
                        IAsyncResultsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite.Add(asyncResultToBeCompleted, null);
                        // spin off waiting for all previous to be notified added
                        previous.BeginWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(cancellationToken, (ar) => {
                            previous.EndWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(ar);
                            // spin off waiting for this to be notified added
                            this.BeginWaitThisBlockProcessedAndAddedToQueueToWrite(cancellationToken, (ar) => {
                                this.EndWaitThisBlockProcessedAndAddedToQueueToWrite(ar);
                                asyncResultToBeCompleted.SetAsCompleted(false);
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
                lock(this)
                {
                    if(!IAsyncResultsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite.ContainsKey(waitingAsyncResult))
                    {
                        throw new InvalidOperationException("End of asynchronius wait request did not originate from a BeginWaitAllPreviousBlocksProcessedAddedToQueueToWrite() method on the current block");
                    }
                }

                try
                {
                    var waitingAsyncResultAsCommonAsyncResultNoResult = waitingAsyncResult as Common.AsyncResultNoResult;
                    if(waitingAsyncResultAsCommonAsyncResultNoResult != null)
                    {
                        waitingAsyncResultAsCommonAsyncResultNoResult.EndInvoke();
                    }
                    else
                    {
                        throw new NotSupportedException("Could not end unexpected but recognized async adding result");
                    }
                }
                finally
                {
                    lock(this)
                    {
                        IAsyncResultsWaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite.Remove(waitingAsyncResult);
                    }
                }
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