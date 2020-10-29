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

        readonly Processors ProcessorsAddToCollection = new Processors();

        public IAsyncResult BeginAdd(T item, CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            if(Implementation.TryAdd(item, 0))
            {
                return ProcessorsAddToCollection.BeginRunCompleted(asyncCallback, state);
            }
            else
            {
                return ProcessorsAddToCollection.BeginRun(() => {
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
                                throw new InvalidOperationException("Can't add item to collection completed for adding");
                            }
                            else
                            {
                                // infinite wait for item to be added is finished but not canceled and collection is not completed for adding
                                throw new NotSupportedException("Infinite wait for item to be added to collection is finished but not canceled");
                            }
                        }
                    }
                }, asyncCallback, state);
            }
        }
        public IAsyncResult BeginAdd(T item, AsyncCallback asyncCallback = null, object state = null)
        {
            return BeginAdd(item, CancellationToken.None, asyncCallback, state);
        }

        public void EndAdd(IAsyncResult addingAsyncResult)
        {
            ProcessorsAddToCollection.EndRun(addingAsyncResult,
                onAsyncResultNotFromThisProcessors: () => {
                    throw new InvalidOperationException("End of asynchronius add request did not originate from a BeginAdd() method on this collection");
                }
            );
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

        readonly Processors<T> ProcessorsTakeFromCollection = new Processors<T>();

        public IAsyncResult BeginTake(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            T item;
            if(Implementation.TryTake(out item, 0))
            {
                return ProcessorsTakeFromCollection.BeginRunCompleted(item, asyncCallback, state);
            }
            else
            {
                return ProcessorsTakeFromCollection.BeginRun(() => {
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
                                throw new InvalidOperationException("Nothing to get out of empty collection completed for adding");
                            }
                            else
                            {
                                // infinite wait for item to be taken is finished but not canceled and collection is not empty
                                throw new NotSupportedException("Infinite wait for item to be taken from collection is finished but not canceled");
                            }
                        }
                    }

                    return item;
                }, asyncCallback, state);
            }
        }
        public IAsyncResult BeginTake(AsyncCallback asyncCallback = null, object state = null)
        {
            return BeginTake(CancellationToken.None, asyncCallback, state);
        }

        public T EndTake(IAsyncResult takingAsyncResult)
        {
            return ProcessorsTakeFromCollection.EndRun(takingAsyncResult,
                onAsyncResultNotFromThisProcessors: () => {
                    throw new InvalidOperationException("End of asynchronius take request did not originate from a BeginTake() method on this collection");
                }
            );
        }
    }
}