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
            return parameter.VerifyNotNullConvertAndRunUnsafe((IAsyncResult addingAsyncResult) =>
                addingAsyncResult.WaitCompletedAndRunUnsafe(Timeout, CancellationTokenSource.Token,
                    whenCompleted: (completedAddingAsyncResult) =>
                    {
                        try
                        {
                            AsyncLimitableCollection.EndAdd(completedAddingAsyncResult);
                            return new PayloadResultContinuationPending();
                        }
                        catch(InvalidOperationException)
                        {
                            if(AsyncLimitableCollection.IsAddingCompleted)
                            {
                                throw new NotSupportedException("Adding to collection was closed prior to adding last block(s)");
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
            ));
        }
    }
}