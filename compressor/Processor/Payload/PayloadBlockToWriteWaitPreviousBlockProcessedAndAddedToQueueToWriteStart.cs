using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadBlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteStart : Common.Payload.Payload
    {
        public PayloadBlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteStart(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (BlockToWrite blockToWait) => 
            {
                var waitingAyncResult = blockToWait.BeginWaitPreviousBlockProcessedAndAddedToQueue(CancellationTokenSource.Token, state: blockToWait);
                return new PayloadResultContinuationPending(waitingAyncResult);
            });
        }
    }
}