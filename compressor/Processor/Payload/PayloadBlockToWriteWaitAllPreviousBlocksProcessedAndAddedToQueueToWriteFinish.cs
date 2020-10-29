using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Processor.Queue;

namespace compressor.Processor.Payload
{
    class PayloadBlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteFinish: Common.Payload.Payload
    {
        public PayloadBlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteFinish(CancellationTokenSource cancellationTokenSource, int waitTimeout)
            : base(cancellationTokenSource)
        {
            this.Timeout = waitTimeout;
        }

        readonly int Timeout;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (IAsyncResult waitingAsyncResult) =>
            {
                return waitingAsyncResult.WaitCompleted<PayloadResult>(Timeout, CancellationTokenSource.Token,
                    whileWaitTimedOut:
                        (incompleteAsyncResult) => new PayloadResultContinuationPendingDoneNothing(),
                    whenCompleted:
                        (completedAsyncResult) =>
                        {
                            var blockToWait = (BlockToWrite)completedAsyncResult.AsyncState;
                            blockToWait.EndWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(completedAsyncResult);
                            return new PayloadResultContinuationPending(blockToWait);
                        }
                );
            });
        }
    }
}