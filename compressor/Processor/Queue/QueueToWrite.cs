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

        public override IAsyncResult BeginAdd(BlockToWrite block, CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            return base.BeginAdd(block, cancellationToken, (ar) => {
                block.NotifyProcessedAndAddedToQueue();
                if(asyncCallback != null)
                {
                    asyncCallback(ar);
                }
            }, state);
        }

        public override IAsyncResult BeginAdd(BlockToWrite block, AsyncCallback asyncCallback = null, object state = null)
        {
            return base.BeginAdd(block, (ar) => {
                block.NotifyProcessedAndAddedToQueue();
                if(asyncCallback != null)
                {
                    asyncCallback(ar);
                }
            }, state);
        }
    }
}