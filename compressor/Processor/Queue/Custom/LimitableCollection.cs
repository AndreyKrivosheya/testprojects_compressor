using System;
using System.Threading;

namespace compressor.Processor.Queue.Custom
{
    class LimitableCollection<T>
    {
        public LimitableCollection(int maxCapacity)
        {
            if(maxCapacity == 0)
            {
                throw new ArgumentException("Can't limit collection to 0 items", "maxCapacity");
            }

            this.MaxCapacity = maxCapacity;
            if(maxCapacity < 1)
            {
                Implementation = new LimitableCollection.ImplementationLimited<T>(this.MaxCapacity);
            }
            else
            {
                Implementation = new LimitableCollection.ImplementationUnlimited<T>();
            }
        }

        public readonly int MaxCapacity;
        readonly LimitableCollection.Implementation<T> Implementation;

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