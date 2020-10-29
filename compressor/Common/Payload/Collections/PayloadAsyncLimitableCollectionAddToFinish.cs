using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Collections;
using compressor.Common.Payload;

namespace compressor.Common.Payload.Collections
{
    class PayloadAsyncLimitableCollectionAddToFinish<T>: PayloadAsyncLimitableCollection<T>
    {
        public PayloadAsyncLimitableCollectionAddToFinish(CancellationTokenSource cancellationTokenSource, AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, asyncLimitableCollection)
        {
            this.Timeout = asyncLimitableCollectionOperationTimeoutMilliseconds;
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
                                AsyncLimitableCollection.EndAdd(completedAsyncResult);
                                return new PayloadResultContinuationPending();
                            }
                            catch(OperationCanceledException)
                            {
                                return new PayloadResultCanceled();
                            }
                            catch(InvalidOperationException)
                            {
                                if(AsyncLimitableCollection.IsAddingCompleted)
                                {
                                    // something wrong: AsyncLimitableCollection is closed for additions, but there's block outstanding
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