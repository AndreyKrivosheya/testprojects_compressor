using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor
{
    class ProcessorQueue
    {
        public ProcessorQueue(int maxCapacity)
        {
            this.QueueMaxCapacity = maxCapacity;
            this.Queue = new BlockingCollection<ProcessorQueueBlock>(this.QueueMaxCapacity);
        }

        readonly BlockingCollection<ProcessorQueueBlock> Queue;
        readonly int QueueMaxCapacity;

        public bool TryAdd(ProcessorQueueBlock item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Queue.TryAdd(item, millisecondsTimeout, cancellationToken);
        }
        public bool TryAdd(ProcessorQueueBlock item, int millisecondsTimeout)
        {
            return Queue.TryAdd(item, millisecondsTimeout);
        }
        public bool TryAdd(ProcessorQueueBlock item, CancellationToken cancellationToken)
        {
            return TryAdd(item, 0, cancellationToken);
        }
        public bool TryAdd(ProcessorQueueBlock item)
        {
            return TryAdd(item, 0);
        }

        public bool IsCompleted
        {
            get
            {
                return Queue.IsCompleted;
            }
        }
        
        public bool IsAddingCompleted
        {
            get
            {
                return Queue.IsAddingCompleted;
            }
        }
        public void CompleteAdding()
        {
            Queue.CompleteAdding();
        }

        public bool TryTake(out ProcessorQueueBlock item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Queue.TryTake(out item, millisecondsTimeout, cancellationToken);
        }
        public bool TryTake(out ProcessorQueueBlock item, int millisecondsTimeout)
        {
            return Queue.TryTake(out item, millisecondsTimeout);
        }
        public bool TryTake(out ProcessorQueueBlock item, CancellationToken cancellationToken)
        {
            return TryTake(out item, 0, cancellationToken);
        }
        public bool TryTake(out ProcessorQueueBlock item)
        {
            return TryTake(out item, 0);
        }

        public bool IsPercentsFull(int percents)
        {
            if(percents < 0 || percents > 100)
                throw new ArgumentException("percents");

            return Queue.Count >= ((percents * this.QueueMaxCapacity) / 100f);
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