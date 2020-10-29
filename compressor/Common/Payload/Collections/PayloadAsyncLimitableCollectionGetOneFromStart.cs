using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Collections;
using compressor.Common.Payload;

namespace compressor.Common.Payload.Collections
{
    class PayloadAsyncLimitableCollectionGetOneFromStart<T>: PayloadAsyncLimitableCollection<T>
    {
        public PayloadAsyncLimitableCollectionGetOneFromStart(CancellationTokenSource cancellationTokenSource, AsyncLimitableCollection<T> asyncLimitableCollection)
            : base(cancellationTokenSource, asyncLimitableCollection)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            try
            {
                var takingAyncResult = AsyncLimitableCollection.BeginTake(CancellationTokenSource.Token);
                return new PayloadResultContinuationPending(takingAyncResult);
            }
            catch(InvalidOperationException)
            {
                if(!AsyncLimitableCollection.IsCompleted)
                {
                    throw;
                }
                else
                {
                    return new PayloadResultContinuationPendingDoneNothing();
                }
            }
        }
    }
}