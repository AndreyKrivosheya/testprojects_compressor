using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue
{
    class QueueToWrite: Queue<BlockToWrite>
    {
        public QueueToWrite(int maxCapacity)
            : base(maxCapacity)
        {
        }

        public override bool TryAdd(BlockToWrite item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if(base.TryAdd(item, millisecondsTimeout, cancellationToken))
            {
                item.NotifyAddedToQueue();
                return true;
            }
            else
            {
                return false;
            }
        }
        public override bool TryAdd(BlockToWrite item, int millisecondsTimeout)
        {
            if(base.TryAdd(item, millisecondsTimeout))
            {
                item.NotifyAddedToQueue();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}