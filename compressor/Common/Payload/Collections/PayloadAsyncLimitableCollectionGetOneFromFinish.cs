using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Collections;
using compressor.Common.Payload;

namespace compressor.Common.Payload.Collections
{
    class PayloadAsyncLimitableCollectionGetOneFromFinish<T>: PayloadAsyncLimitableCollection<T>
    {
        public PayloadAsyncLimitableCollectionGetOneFromFinish(CancellationTokenSource cancellationTokenSource, AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, asyncLimitableCollection)
        {
            this.Timeout = asyncLimitableCollectionOperationTimeoutMilliseconds;
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
                                var blockTaken = AsyncLimitableCollection.EndTake(completedAsyncResult);
                                return new PayloadResultContinuationPending(blockTaken);
                            }
                            catch(OperationCanceledException)
                            {
                                return new PayloadResultCanceled();
                            }
                            catch(InvalidOperationException)
                            {
                                if(AsyncLimitableCollection.IsCompleted)
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