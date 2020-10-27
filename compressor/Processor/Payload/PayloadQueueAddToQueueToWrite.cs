using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueAddToQueueToWrite: PayloadQueueAddTo<BlockToWrite>
    {
        public PayloadQueueAddToQueueToWrite(CancellationTokenSource cancellationTokenSource, QueueToWrite queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, queue, queueOperationTimeoutMilliseconds)
        {
        }

        protected override PayloadResult RunUnsafe(BlockToWrite blockToAdd)
        {
            if(blockToAdd.WaitAllPreviousBlocksProcessedAndAddedToQueue(Timeout, CancellationTokenSource.Token))
            {
                return base.RunUnsafe(blockToAdd);
            }
            else
            {
                return new PayloadResultContinuationPendingDoneNothing();
            }
        }
    }
}