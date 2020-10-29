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
                    whileWaitTimedOut:
                        (incompleteAsyncResult) => new PayloadResultContinuationPendingDoneNothing(),
                    whenCompleted:
                        (completedAsyncResult) =>
                        {
                            try
                            {
                                Queue.EndAdd(completedAsyncResult);
                                return new PayloadResultContinuationPending();
                            }
                            catch(OperationCanceledException)
                            {
                                return new PayloadResultCanceled();
                            }
                            catch(InvalidOperationException)
                            {
                                if(Queue.IsAddingCompleted)
                                {
                                    // something wrong: queue is closed for additions, but there's block outstanding
                                    // probably there's an exception on another worker thread
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