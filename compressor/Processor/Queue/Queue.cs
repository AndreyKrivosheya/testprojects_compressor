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
            this.QueueMaxCapacity = maxCapacity;
            this.QueueImplementation = new BlockingCollection<TBlock>(new ConcurrentQueue<TBlock>(), maxCapacity);
        }

        readonly BlockingCollection<TBlock> QueueImplementation;
        readonly int QueueMaxCapacity;

        public virtual bool TryAdd(TBlock item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return QueueImplementation.TryAdd(item, millisecondsTimeout, cancellationToken);
        }
        public virtual bool TryAdd(TBlock item, int millisecondsTimeout)
        {
            return QueueImplementation.TryAdd(item, millisecondsTimeout);
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
                return QueueImplementation.IsCompleted;
            }
        }
        
        public bool IsAddingCompleted
        {
            get
            {
                return QueueImplementation.IsAddingCompleted;
            }
        }
        public virtual bool CompleteAdding(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            QueueImplementation.CompleteAdding();
            return true;
        }
        public virtual bool CompleteAdding(int millisecondsTimeout)
        {
            QueueImplementation.CompleteAdding();
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
            return QueueImplementation.TryTake(out item, millisecondsTimeout, cancellationToken);
        }
        public bool TryTake(out TBlock item, int millisecondsTimeout)
        {
            return QueueImplementation.TryTake(out item, millisecondsTimeout);
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

            return QueueImplementation.Count >= ((percents * this.QueueMaxCapacity) / 100f);
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