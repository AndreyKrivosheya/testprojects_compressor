using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Processor.Queue;

namespace compressor.Processor.Payload
{
    class PayloadQueueGetOneFromFinish<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueGetOneFromFinish(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, queue)
        {
            this.Timeout = queueOperationTimeoutMilliseconds;
        }

        readonly int Timeout;
        
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (IAsyncResult takingAsyncResult) =>
            {
                return takingAsyncResult.WaitCompleted<PayloadResult>(Timeout, CancellationTokenSource.Token,
                    whileWaitTimedOut:
                        (incompleteAsyncResult) => new PayloadResultContinuationPendingDoneNothing(),
                    whenCompleted:
                        (completedAsyncResult) =>
                        {
                            try
                            {
                                var blockTaken = Queue.EndTake(completedAsyncResult);
                                return new PayloadResultContinuationPending(blockTaken);
                            }
                            catch(OperationCanceledException)
                            {
                                return new PayloadResultCanceled();
                            }
                            catch(InvalidOperationException)
                            {
                                if(Queue.IsCompleted)
                                {
                                    return new PayloadResultSucceeded();
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                );
            });
        }
    }
}