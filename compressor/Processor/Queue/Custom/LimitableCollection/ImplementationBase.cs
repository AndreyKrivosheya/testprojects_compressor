
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue.Custom.LimitableCollection
{
    abstract class ImplementationBase<T> : Implementation<T>
    {
        public ImplementationBase()
        {
            this.ConcurrentCollection = new ConcurrentQueue<T>();
        }

        protected readonly ConcurrentQueue<T> ConcurrentCollection;

        public int Count
        {
            get
            {
                return ConcurrentCollection.Count;
            }
        }

        public abstract bool IsCompleted { get; }
        
        public abstract bool IsAddingCompleted { get; }

        public abstract bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken);
        public abstract bool TryAdd(T item, int millisecondsTimeout);

        public abstract bool CompleteAdding();
       
        public abstract bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken);
        public abstract bool TryTake(out T item, int millisecondsTimeout);
    }
}