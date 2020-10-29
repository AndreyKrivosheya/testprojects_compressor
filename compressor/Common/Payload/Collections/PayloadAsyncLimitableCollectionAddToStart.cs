using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Collections;
using compressor.Common.Payload;

namespace compressor.Common.Payload.Collections
{
    class PayloadAsyncLimitableCollectionAddToStart<T>: PayloadAsyncLimitableCollection<T>
    {
        public PayloadAsyncLimitableCollectionAddToStart(CancellationTokenSource cancellationTokenSource, AsyncLimitableCollection<T> asyncLimitableCollection)
            : base(cancellationTokenSource, asyncLimitableCollection)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (T itemToAdd) => 
            {
                try
                {
                    var addingAyncResult = AsyncLimitableCollection.BeginAdd(itemToAdd, CancellationTokenSource.Token);
                    return new PayloadResultContinuationPending(addingAyncResult);
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
            });
        }
    }
}