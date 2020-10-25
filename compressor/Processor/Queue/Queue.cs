using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue
{
    class Queue<TBlock>
        where TBlock: Block
    {
        public Queue(int maxCapacity)
        {
            this.MaxCapacity = maxCapacity;
            this.Implementation = new BlockingCollection<TBlock>(new ConcurrentQueue<TBlock>(), maxCapacity);
        }

        readonly BlockingCollection<TBlock> Implementation;
        public readonly int MaxCapacity;

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
        public virtual bool CompleteAdding(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            Implementation.CompleteAdding();
            return true;
        }
        public virtual bool CompleteAdding(int millisecondsTimeout)
        {
            Implementation.CompleteAdding();
            return true;
        }
        public bool CompleteAdding(CancellationToken cancellationToken)
        {
            return CompleteAdding(0, cancellationToken);
        }
        public bool CompleteAdding()
        {
            return CompleteAdding(0);
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

            return Implementation.Count >= ((percents * MaxCapacity) / 100f);
        }
        public bool IsHalfFull()
        {
            return IsPercentsFull(50);
        }
        public bool IsAlmostFull()
        {
            return IsPercentsFull(90);
        }
    }
}