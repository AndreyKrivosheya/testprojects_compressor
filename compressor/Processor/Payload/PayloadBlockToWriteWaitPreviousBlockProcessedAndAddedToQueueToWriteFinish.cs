using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Processor.Queue;

namespace compressor.Processor.Payload
{
    class PayloadBlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteFinish: Common.Payload.Payload
    {
        public PayloadBlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteFinish(CancellationTokenSource cancellationTokenSource, int waitTimeout)
            : base(cancellationTokenSource)
        {
            this.Timeout = waitTimeout;
        }

        readonly int Timeout;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((IAsyncResult waitingAsyncResult) =>
                waitingAsyncResult.WaitCompletedAndRunUnsafe(Timeout, CancellationTokenSource.Token,
                    whenCompleted: (completedWaitingAsyncResult) =>
                    {
                        var blockToWait = (BlockToWrite)completedWaitingAsyncResult.AsyncState;
                        blockToWait.EndWaitPreviousBlockProcessedAndAddedToQueueToWrite(completedWaitingAsyncResult);
                        return new PayloadResultContinuationPending(blockToWait);
                    }
                )
            );
        }
    }
}