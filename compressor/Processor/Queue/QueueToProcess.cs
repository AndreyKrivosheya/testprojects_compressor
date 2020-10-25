using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue
{
    // implies only synchronious additions, so that queue is capable of tracking
    // of order of blocks insertion to determine block last added
    class QueueToProcess: Queue<BlockToProcess>
    {
        public QueueToProcess(int maxCapacity)
            : base(Math.Max(1, maxCapacity - 1))
        {
        }

        BlockToProcess Last = null;

        public override bool TryAdd(BlockToProcess item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if(Last == null)
            {
                Last = item;
                return true;
            }
            else
            {
                if(base.TryAdd(Last, millisecondsTimeout, cancellationToken))
                {
                    Last = item;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public override bool TryAdd(BlockToProcess item, int millisecondsTimeout)
        {
            if(Last == null)
            {
                Last = item;
                return true;
            }
            else
            {
                if(base.TryAdd(Last, millisecondsTimeout))
                {
                    Last = item;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override bool CompleteAdding(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // notify last block that it's last
            if(Last != null)
            {
                Last.NotifyLast();
                // try adding last block
                if(!TryAdd(null, millisecondsTimeout, cancellationToken))
                {
                    return false;
                }
            }
            base.CompleteAdding(millisecondsTimeout);
            return true;
        }
        public override bool CompleteAdding(int millisecondsTimeout)
        {
            // notify last block that it's last
            if(Last != null)
            {
                Last.NotifyLast();
                // try adding last block
                if(!TryAdd(null, millisecondsTimeout))
                {
                    return false;
                }
            }
            base.CompleteAdding(millisecondsTimeout);
            return true;
        }
    }
}