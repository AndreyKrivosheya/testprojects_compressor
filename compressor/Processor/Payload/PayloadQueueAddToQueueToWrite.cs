using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueAddToQueueToWrite: PayloadQueueAddTo<BlockToWrite>
    {
        public PayloadQueueAddToQueueToWrite(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, QueueToWrite queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, settings, queue, queueOperationTimeoutMilliseconds)
        {
        }

        protected override PayloadResult RunUnsafe(BlockToWrite blockToAdd)
        {
            if(blockToAdd.WaitAllPreviousBlocksAddedToQueue(Timeout, CancellationTokenSource.Token))
            {
                var addResult = base.RunUnsafe(blockToAdd);
                // if block was actually added
                if(addResult.Status == PayloadResultStatus.ContinuationPending)
                {
                    // if last block was added
                    if(blockToAdd.Last)
                    {
                        return new PayloadResultSucceeded(PayloadQueueCompleteAdding.LastObjectAdded);
                    }
                }

                return addResult;
            }
            else
            {
                return new PayloadResultContinuationPendingDoneNothing();
            }
        }
    }
}