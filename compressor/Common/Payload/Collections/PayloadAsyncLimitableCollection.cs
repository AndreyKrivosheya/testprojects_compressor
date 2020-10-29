using System;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Common.Collections;

namespace compressor.Common.Payload.Collections
{
    abstract class PayloadAsyncLimitableCollection<T>: Common.Payload.Payload
    {
        public PayloadAsyncLimitableCollection(CancellationTokenSource cancellationTokenSource, AsyncLimitableCollection<T> asyncLimitableCollection)
            : base(cancellationTokenSource)
        {
            this.AsyncLimitableCollection = asyncLimitableCollection;
        }

        protected readonly AsyncLimitableCollection<T> AsyncLimitableCollection;
    }
}