using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue
{
    class Queue<TBlock> : IDisposable
        where TBlock: Block
    {
        public Queue(int maxCapacity)
        {
            if(maxCapacity < 1)
            {
                throw new ArgumentException("Can't limit collection to less than 1 item", "maxCapacity");
            }

            this.Implementation = new Custom.LimitableCollection<TBlock, ConcurrentQueue<TBlock>>(maxCapacity);
        }

        readonly Custom.LimitableCollection<TBlock> Implementation;

        public void Dispose()
        {
            Implementation.Dispose();
        }
        
        public virtual bool TryAdd(TBlock item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Implementation.TryAdd(item, millisecondsTimeout, cancellationToken);
        }
        public virtual bool TryAdd(TBlock item, int millisecondsTimeout)
        {
            return Implementation.TryAdd(item, millisecondsTimeout);
        }
        public bool TryAdd(TBlock item, CancellationToken cancellationToken)
        {
            return TryAdd(item, 0, cancellationToken);
        }
        public bool TryAdd(TBlock item)
        {
            return TryAdd(item, 0);
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
        
        public void CompleteAdding()
        {
            Implementation.CompleteAdding();
        }

        public bool TryTake(out TBlock item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Implementation.TryTake(out item, millisecondsTimeout, cancellationToken);
        }
        public bool TryTake(out TBlock item, int millisecondsTimeout)
        {
            return Implementation.TryTake(out item, millisecondsTimeout);
        }
        public bool TryTake(out TBlock item, CancellationToken cancellationToken)
        {
            return TryTake(out item, 0, cancellationToken);
        }
        public bool TryTake(out TBlock item)
        {
            return TryTake(out item, 0);
        }

        public bool IsPercentsFull(int percents)
        {
            if(percents < 0 || percents > 100)
                throw new ArgumentException("percents");
            if(Implementation.MaxCapacity < 1)
            {
                return false;
            }
            else
            {
                return Implementation.Count >= ((percents * Implementation.MaxCapacity) / 100f);
            }
        }

        public bool IsHalfFull()
        {
            return IsPercentsFull(50);
        }
        public bool IsAlmostFull()
        {
            return IsPercentsFull(90);
        }
        public bool IsFull()
        {
            return IsPercentsFull(99);
        }
    }
}