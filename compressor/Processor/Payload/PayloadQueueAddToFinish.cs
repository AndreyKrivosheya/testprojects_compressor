using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Processor.Queue;

namespace compressor.Processor.Payload
{
    class PayloadQueueAddToFinish<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueAddToFinish(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, queue)
        {
            this.Timeout = queueOperationTimeoutMilliseconds;
        }

        readonly int Timeout;
        
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (IAsyncResult addingAsyncResult) =>
            {
                return addingAsyncResult.WaitCompleted<PayloadResult>(Timeout, CancellationTokenSource.Token,
                    whenWaitTimedOut:
                        (incompleteAsyncResult) => new PayloadResultContinuationPendingDoneNothing(),
                    whenCompleted:
                        (completedAsyncResult) =>
                        {
                            Queue.EndAdd(completedAsyncResult);
                            return new PayloadResultContinuationPending();
                        }
                );
            });
        }
    }
}