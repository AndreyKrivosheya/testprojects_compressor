using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Common.Collections
{
    interface AsyncLimitableCollection<T>: IDisposable
    {
        int MaxCapacity { get; }
        int Count { get; }

        bool IsCompleted { get; }
        
        bool IsAddingCompleted { get; }


        IAsyncResult BeginAdd(T item, CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null);
        IAsyncResult BeginAdd(T item, AsyncCallback asyncCallback = null, object state = null);
        void EndAdd(IAsyncResult addingAsyncResult);

        void CompleteAdding();
        
        IAsyncResult BeginTake(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null);
        IAsyncResult BeginTake(AsyncCallback asyncCallback = null, object state = null);
        T EndTake(IAsyncResult takingAsyncResult);
    };

    class AsyncLimitableCollection<T, TCollection>: AsyncLimitableCollection<T>
        where TCollection: LimitableCollection<T>
    {
        public AsyncLimitableCollection(int maxCapacity)
        {
            try
            {
                this.Implementation = (TCollection)Activator.CreateInstance(typeof(TCollection), maxCapacity);
            }
            catch(Exception)
            {
                throw;
            }
        }

        readonly TCollection Implementation;

        public virtual void Dispose()
        {
            Implementation.Dispose();
        }

        public int MaxCapacity
        {
            get
            {
                return Implementation.MaxCapacity;
            }
        }

        public int Count
        {
            get
            {
                return Implementation.Count;
            }
        }

        readonly Dictionary<IAsyncResult, Processor> ProcessorsAddToQueue = new Dictionary<IAsyncResult, Processor>();
        
        public IAsyncResult BeginAdd(T item, CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            Processor processor;
            IAsyncResult processorAsyncResult;
            {
                if(Implementation.TryAdd(item, 0))
                {
                    var
                    asyncResultCompleted = new Common.AsyncResultNoResult(asyncCallback, state);
                    asyncResultCompleted.SetAsCompleted(true);

                    processor = null;
                    processorAsyncResult = asyncResultCompleted;
                }
                else
                {
                    processor = new ProcessorViaAction(() => {
                        if(!Implementation.TryAdd(item, Timeout.Infinite, cancellationToken))
                        {
                            if(cancellationToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException();
                            }
                            else
                            {
                                if(Implementation.IsAddingCompleted)
                                {
                                    throw new InvalidOperationException("Can't add item to queue completed for adding");
                                }
                                else
                                {
                                    // infinite wait for item to be added is finished but not canceled and queue is not completed for adding
                                    throw new NotSupportedException("Infinite wait for item to be added to collection is finished but not canceled");
                                }
                            }
                        }
                    });
                    processorAsyncResult = processor.BeginRun(asyncCallback, state);
                }
            }

            lock(ProcessorsAddToQueue)
            {
                ProcessorsAddToQueue.Add(processorAsyncResult, processor);
            }
            
            return processorAsyncResult;
        }
        public IAsyncResult BeginAdd(T item, AsyncCallback asyncCallback = null, object state = null)
        {
            return BeginAdd(item, CancellationToken.None, asyncCallback, state);
        }

        public void EndAdd(IAsyncResult addingAsyncResult)
        {
            Processor processor;
            lock(ProcessorsAddToQueue)
            {
                if(!ProcessorsAddToQueue.TryGetValue(addingAsyncResult, out processor))
                {
                    throw new InvalidOperationException("End of asynchronius add request did not originate from a BeginAdd() method on the current queue");
                }
            }

            try
            {
                if(processor == null)
                {
                    var addingAsyncResultAsCommonAsyncResultNoResult = addingAsyncResult as Common.AsyncResultNoResult;
                    if(addingAsyncResultAsCommonAsyncResultNoResult != null && addingAsyncResult.CompletedSynchronously)
                    {
                        addingAsyncResultAsCommonAsyncResultNoResult.EndInvoke();
                    }
                    else
                    {
                        throw new NotSupportedException("Could not end unexpected but recognized async adding result");
                    }
                }
                else
                {
                    processor.EndRun(addingAsyncResult);
                }
            }
            finally
            {
                lock(ProcessorsAddToQueue)
                {
                    ProcessorsAddToQueue.Remove(addingAsyncResult);
                }
            }
        }


        public bool IsAddingCompleted
        {
            get
            {
                return Implementation.IsAddingCompleted;
            }
        }

        public void CompleteAdding()
        {
            Implementation.CompleteAdding();
        }

        public bool IsCompleted
        {
            get
            {
                return Implementation.IsCompleted;
            }
        }

        readonly Dictionary<IAsyncResult, ProcessorWithResult<T>> ProcessorsTakeFromQueue = new Dictionary<IAsyncResult, ProcessorWithResult<T>>();
        
        public IAsyncResult BeginTake(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            ProcessorWithResult<T> processor;
            IAsyncResult processorAsyncResult;
            {
                T item;
                if(Implementation.TryTake(out item, 0))
                {
                    var
                    asyncResultCompleted = new Common.AsyncResult<T>(asyncCallback, state);
                    asyncResultCompleted.SetAsCompleted(item, true);

                    processor = null;
                    processorAsyncResult = asyncResultCompleted;
                }
                else
                {
                    processor = new ProcessorWithResultViaFunc<T>(() => {
                        T item;
                        if(!Implementation.TryTake(out item, Timeout.Infinite, cancellationToken))
                        {
                            if(cancellationToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException();
                            }
                            else
                            {
                                if(Implementation.IsCompleted)
                                {
                                    throw new InvalidOperationException("Nothing to get out of empty queue completed for adding");
                                }
                                else
                                {
                                    // infinite wait for item to be taken is finished but not canceled and queue is not empty
                                    throw new NotSupportedException("Infinite wait for item to be taken from collection is finished but not canceled");
                                }
                            }
                        }

                        return item;
                    });
                    processorAsyncResult = processor.BeginRun(asyncCallback, state);
                }
            }

            lock(ProcessorsTakeFromQueue)
            {
                ProcessorsTakeFromQueue.Add(processorAsyncResult, processor);
            }
            return processorAsyncResult;
        }
        public IAsyncResult BeginTake(AsyncCallback asyncCallback = null, object state = null)
        {
            return BeginTake(CancellationToken.None, asyncCallback, state);
        }

        public T EndTake(IAsyncResult takingAsyncResult)
        {
            ProcessorWithResult<T> processor;
            lock(ProcessorsTakeFromQueue)
            {
                if(!ProcessorsTakeFromQueue.TryGetValue(takingAsyncResult, out processor))
                {
                    throw new InvalidOperationException("End of asynchronius take request did not originate from a BeginTake() method on the current queue");
                }
            }

            try
            {
                if(processor == null)
                {
                    var takingAsyncResultAsCommonAsyncResult = takingAsyncResult as Common.AsyncResult<T>;
                    if(takingAsyncResultAsCommonAsyncResult != null && takingAsyncResult.CompletedSynchronously)
                    {
                        return takingAsyncResultAsCommonAsyncResult.EndInvoke();
                    }
                    else
                    {
                        throw new NotSupportedException("Could not end unexpected but recognized async taking result");
                    }
                }
                else
                {
                    return processor.EndRun(takingAsyncResult);
                }
            }
            finally
            {
                lock(ProcessorsTakeFromQueue)
                {
                    ProcessorsTakeFromQueue.Remove(takingAsyncResult);
                }
            }
        }
    }
}