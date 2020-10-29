using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Collections;
using compressor.Common.Payload;

namespace compressor.Common.Payload.Collections
{
    class PayloadAsyncLimitableCollectionCompleteAdding<T>: PayloadAsyncLimitableCollection<T>
    {
        public PayloadAsyncLimitableCollectionCompleteAdding(CancellationTokenSource cancellationTokenSource, AsyncLimitableCollection<T> asyncLimitableCollection)
            : base(cancellationTokenSource, asyncLimitableCollection)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            AsyncLimitableCollection.CompleteAdding();
            return new PayloadResultContinuationPending();
        }
    }
}