using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor
{
    class ProcessorQueueToWrite: ProcessorQueue<ProcessorQueueBlockToWrite>
    {
        public ProcessorQueueToWrite(int maxCapacity)
            : base(maxCapacity)
        {
        }

        public override bool TryAdd(ProcessorQueueBlockToWrite item, int millisecondsTimeout, CancellationToken cancellationToken)
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
        public override bool TryAdd(ProcessorQueueBlockToWrite item, int millisecondsTimeout)
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