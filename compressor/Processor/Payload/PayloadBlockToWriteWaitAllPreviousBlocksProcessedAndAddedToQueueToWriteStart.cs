using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadBlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteStart : Common.Payload.Payload
    {
        public PayloadBlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteStart(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (BlockToWrite blockToWait) => 
            {
                var waitingAyncResult = blockToWait.BeginWaitAllPreviousBlocksProcessedAndAddedToQueue(CancellationTokenSource.Token, state: blockToWait);
                return new PayloadResultContinuationPending(waitingAyncResult);
            });
        }
    }
}