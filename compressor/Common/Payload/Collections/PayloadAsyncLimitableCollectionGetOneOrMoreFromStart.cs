using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common;
using compressor.Common.Collections;
using compressor.Common.Payload;

namespace compressor.Common.Payload.Collections
{
    class PayloadAsyncLimitableCollectionGetOneOrMoreFromStart<T>: PayloadAsyncLimitableCollection<T>
    {
        public PayloadAsyncLimitableCollectionGetOneOrMoreFromStart(CancellationTokenSource cancellationTokenSource, AsyncLimitableCollection<T> asyncLimitableCollection)
            : base(cancellationTokenSource, asyncLimitableCollection)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (int maxBlocksToGet) =>
            {
                try
                {
                    var takingAyncResult = AsyncLimitableCollection.BeginTake(CancellationTokenSource.Token, state: maxBlocksToGet);
                    return new PayloadResultContinuationPending(takingAyncResult);
                }
                catch(InvalidOperationException)
                {
                    if(AsyncLimitableCollection.IsCompleted)
                    {
                        return new PayloadResultContinuationPendingDoneNothing();
                    }

                    throw;
                }
            });
        }
    }
}