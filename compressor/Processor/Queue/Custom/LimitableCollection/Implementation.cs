using System;
using System.Threading;

namespace compressor.Processor.Queue.Custom.LimitableCollection
{
    interface Implementation<T> : IDisposable
    {
        int Count { get; }

        bool IsCompleted { get; }
        
        bool IsAddingCompleted { get; }

        bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken);
        bool TryAdd(T item, int millisecondsTimeout);

        void CompleteAdding();
       
        bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken);
        bool TryTake(out T item, int millisecondsTimeout);
    }
}