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
                catch(OperationCanceledException)
                {
                    return new PayloadResultCanceled();
                }
                catch(InvalidOperationException)
                {
                    if(AsyncLimitableCollection.IsAddingCompleted)
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
            });
        }
    }
}