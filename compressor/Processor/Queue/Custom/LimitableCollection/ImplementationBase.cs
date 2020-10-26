using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue.Custom.LimitableCollection
{
    abstract class ImplementationBase<T, TCollection> : Implementation<T>
        where TCollection: IProducerConsumerCollection<T>, new()
    {
        public ImplementationBase()
        {
            this.ConcurrentCollection = new TCollection();
            this.ConsumersSemaphore = new Semaphore(0, Int32.MaxValue);
            this.ConsumersCancellationTokenSource = new CancellationTokenSource();
        }

        protected readonly IProducerConsumerCollection<T> ConcurrentCollection;

        public int Count
        {
            get
            {
                return ConcurrentCollection.Count;
            }
        }

        // amount of threads currently trying to add to collection
        protected volatile int CurrentAddersCount = 0;
        // mask to indicate that adding to collection is completed
        protected readonly int CurrentAddersCountAddingCompletedMask = unchecked((int)0x80000000);

        readonly Semaphore ConsumersSemaphore;
        readonly CancellationTokenSource ConsumersCancellationTokenSource;


        bool IsDisposed = false;
        void ThrowIfDisposed()
        {
            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().AssemblyQualifiedName);
            }
        }
        
        protected virtual void DisposeCollection()
        {
            ConsumersCancellationTokenSource.Dispose();
        }
        public void Dispose()
        {
            ThrowIfDisposed();
            DisposeCollection();
        }

        public bool IsAddingCompleted
        {
            get
            {
                ThrowIfDisposed();
                return CurrentAddersCount == CurrentAddersCountAddingCompletedMask;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return IsAddingCompleted && Count == 0;
            }
        }

        protected virtual bool TryAddToCollection(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // either adding to collection would be completed
            // or this TryAdd will actually try adding to collection
            var awaiter = new SpinWait();
            while(true)
            {
                var observedCurrentAddersCount = CurrentAddersCount;
                // if adding was completed
                if((observedCurrentAddersCount & CurrentAddersCountAddingCompletedMask) != 0)
                {
                    // ... wait till all the adders completed
                    awaiter.Reset();
                    while((CurrentAddersCount & ~CurrentAddersCount) != 0)
                    {
                        awaiter.SpinOnce();
                    }
                    // and throw InvalidOpeationException due to concurrent TryAdd and CompleteAdding are not supoorted
                    throw new InvalidOperationException("Can't add to collection for which adding was completed");
                }
                else
                {
                    // if this TryAdd is actually trying adding to collection
                    if(Interlocked.CompareExchange(ref CurrentAddersCount, observedCurrentAddersCount + 1, observedCurrentAddersCount) == observedCurrentAddersCount)
                    {
                        try
                        {
                            if(observedCurrentAddersCount + 1 == ~CurrentAddersCountAddingCompletedMask)
                            {
                                throw new InvalidOperationException("Concurrent adders amount exceeded");
                            }
                            if(cancellationToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException();
                            }
                            
                            if(ConcurrentCollection.TryAdd(item))
                            {
                                ConsumersSemaphore.Release();
                                return true;
                            }
                            else
                            {
                                throw new InvalidOperationException("Failed to add to underlaying collection");
                            }
                        }
                        finally
                        {
                            Interlocked.Decrement(ref CurrentAddersCount);
                        }
                    }
                }
                awaiter.SpinOnce();
            }
        }
        public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if(cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            if(IsAddingCompleted)
            {
                throw new InvalidOperationException("Can't add to collection for which adding was completed");
            }

            return TryAddToCollection(item, millisecondsTimeout, cancellationToken);
        }
        public bool TryAdd(T item, int millisecondsTimeout)
        {
            return TryAdd(item, millisecondsTimeout, CancellationToken.None);
        }

        protected virtual bool CompleteAddingToCollection()
        {
            var awaiter = new SpinWait();
            while(true)
            {
                var observerCurrentAddersCount = CurrentAddersCount;
                // if concurrent CompleteAdding is in progress waiting for adders to finish ...
                if((observerCurrentAddersCount & CurrentAddersCountAddingCompletedMask) != 0)
                {
                    // ... then wait all adders are finished
                    awaiter.Reset();
                    while((CurrentAddersCount & ~CurrentAddersCountAddingCompletedMask) != 0)
                    {
                        awaiter.SpinOnce();
                    }
                    return false;
                }
                else
                {
                    // ... if this CompleteAdding actually completed adding
                    if(Interlocked.CompareExchange(ref CurrentAddersCount, CurrentAddersCount | CurrentAddersCountAddingCompletedMask, observerCurrentAddersCount) == observerCurrentAddersCount)
                    {
                        // ... ... then spin util all adders have finished
                        awaiter.Reset();
                        while((CurrentAddersCount & ~CurrentAddersCountAddingCompletedMask) != 0)
                        {
                            awaiter.SpinOnce();
                        }

                        // wake up takers
                        ConsumersCancellationTokenSource.Cancel();

                        return true;
                    }
                }
                awaiter.SpinOnce();
            }
        }
        public void CompleteAdding()
        {
            ThrowIfDisposed();
            if(IsAddingCompleted)
            {
                return;
            }

            CompleteAddingToCollection();
        }

        protected virtual bool TryTakeFromCollection(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            item = default(T);
            // try getting semaphore with no wait if possible or go for full waiting
            var waitSucceeded = ConsumersSemaphore.WaitOne(0);
            if(!waitSucceeded && millisecondsTimeout != 0)
            {
                var waitResult = WaitHandle.WaitAny(new [] { ConsumersSemaphore, cancellationToken.WaitHandle, ConsumersCancellationTokenSource.Token.WaitHandle }, millisecondsTimeout);
                if(waitResult == WaitHandle.WaitTimeout)
                {
                    return false;
                }
                else
                {
                    switch(waitResult)
                    {
                        case 0:
                            waitSucceeded = true;
                            break;
                        case 1:
                            // canceled through external cancellation token
                            throw new OperationCanceledException();
                        case 2:
                        default:
                            // canceled throuhg internal cancellation token due to CompleteAdding
                            return false;
                    }
                }
            }

            if(waitSucceeded)
            {
                try
                {
                    if(!ConcurrentCollection.TryTake(out item))
                    {
                        throw new InvalidOperationException("Failed to take out of underlaying collection");
                    }
                    else
                    {
                        return true;
                    }
                }
                catch(Exception)
                {
                    ConsumersSemaphore.Release();
                    throw;
                }
            }
            else
            {
                return false;
            }
        }       
        public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            item = default(T);
            ThrowIfDisposed();

            if(cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
            if(IsCompleted)
            {
                throw new InvalidOperationException("Can't take out of empty collection for which adding is completed");
            }

            return TryTakeFromCollection(out item, millisecondsTimeout, cancellationToken);
        }
        public bool TryTake(out T item, int millisecondsTimeout)
        {
            return TryTake(out item, millisecondsTimeout, CancellationToken.None);
        }
    }
}