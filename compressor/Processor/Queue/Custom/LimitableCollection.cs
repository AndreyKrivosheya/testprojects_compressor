using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue.Custom
{
    interface LimitableCollection<T> : IDisposable
    {
        int MaxCapacity { get; }
        int Count { get; }

        bool IsCompleted { get; }
        
        bool IsAddingCompleted { get; }

        bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken);
        bool TryAdd(T item, int millisecondsTimeout);

        void CompleteAdding();
       
        bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken);
        bool TryTake(out T item, int millisecondsTimeout);
    }

    class LimitableCollection<T, TCollection> : LimitableCollection<T>, IDisposable
        where TCollection: IProducerConsumerCollection<T>, new()
    {
        public LimitableCollection(int maxCapacity)
        {
            if(maxCapacity == 0)
            {
                throw new ArgumentException("Can't limit collection to 0 items", "maxCapacity");
            }

            if(maxCapacity < 1)
            {
                Implementation = new LimitableCollection.ImplementationUnlimited<T, TCollection>();
            }
            else
            {
                Implementation = new LimitableCollection.ImplementationLimited<T, TCollection>(maxCapacity);
            }
        }

        readonly LimitableCollection.Implementation<T> Implementation;

        public void Dispose()
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

        public bool IsCompleted
        {
            get
            {
                return Implementation.IsCompleted;
            }
        }
        
        public bool IsAddingCompleted
        {
            get
            {
                return Implementation.IsAddingCompleted;
            }
        }

        public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Implementation.TryAdd(item, millisecondsTimeout, cancellationToken);
        }
        public bool TryAdd(T item, int millisecondsTimeout)
        {
            return Implementation.TryAdd(item, millisecondsTimeout);
        }

        public void CompleteAdding()
        {
            Implementation.CompleteAdding();
        }

        public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Implementation.TryTake(out item, millisecondsTimeout, cancellationToken);
        }
        public bool TryTake(out T item, int millisecondsTimeout)
        {
            return Implementation.TryTake(out item, millisecondsTimeout);
        }
    }
}