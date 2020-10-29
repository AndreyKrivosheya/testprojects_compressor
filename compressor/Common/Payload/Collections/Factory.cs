using System.IO;
using System.Threading;

using compressor.Common.Collections;

namespace compressor.Common.Payload.Collections
{
    class Factory
    {
        public Factory(CancellationTokenSource cancellationTokenSource)
        {
            this.CancellationTokenSource = cancellationTokenSource;
            this.FactoryBasic = new Common.Payload.Basic.Factory(this.CancellationTokenSource);
        }

        readonly CancellationTokenSource CancellationTokenSource;

        readonly Common.Payload.Basic.Factory FactoryBasic;

        public PayloadAsyncLimitableCollectionAddToStart<T> AsyncLimitableCollectionAddToStart<T>(AsyncLimitableCollection<T> asyncLimitableCollection)
        {
            return new PayloadAsyncLimitableCollectionAddToStart<T>(CancellationTokenSource, asyncLimitableCollection);
        }
        public PayloadAsyncLimitableCollectionAddToFinish<T> AsyncLimitableCollectionAddToFinish<T>(AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds)
        {
            return new PayloadAsyncLimitableCollectionAddToFinish<T>(CancellationTokenSource, asyncLimitableCollection, asyncLimitableCollectionOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload AsyncLimitableCollectionAddTo<T>(AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain(
                AsyncLimitableCollectionAddToStart<T>(asyncLimitableCollection),
                AsyncLimitableCollectionAddToFinish<T>(asyncLimitableCollection, asyncLimitableCollectionOperationTimeoutMilliseconds)
            );
        }

        public PayloadAsyncLimitableCollectionCompleteAdding<T> AsyncLimitableCollectionCompleteAdding<T>(AsyncLimitableCollection<T> asyncLimitableCollection)
        {
            return new PayloadAsyncLimitableCollectionCompleteAdding<T>(CancellationTokenSource, asyncLimitableCollection);
        }

        public PayloadAsyncLimitableCollectionGetOneFromStart<T> AsyncLimitableCollectionGetOneFromStart<T>(AsyncLimitableCollection<T> asyncLimitableCollection)
        {
            return new PayloadAsyncLimitableCollectionGetOneFromStart<T>(CancellationTokenSource, asyncLimitableCollection);
        }
        public PayloadAsyncLimitableCollectionGetOneFromFinish<T> AsyncLimitableCollectionGetOneFromFinish<T>(AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds)
        {
            return new PayloadAsyncLimitableCollectionGetOneFromFinish<T>(CancellationTokenSource, asyncLimitableCollection, asyncLimitableCollectionOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload AsyncLimitableCollectionGetOneFrom<T>(AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain( 
                AsyncLimitableCollectionGetOneFromStart<T>(asyncLimitableCollection),
                AsyncLimitableCollectionGetOneFromFinish<T>(asyncLimitableCollection, asyncLimitableCollectionOperationTimeoutMilliseconds)
            );
        }

        public PayloadAsyncLimitableCollectionGetOneOrMoreFromStart<T> AsyncLimitableCollectionGetOneOrMoreFromStart<T>(AsyncLimitableCollection<T> asyncLimitableCollection)
        {
            return new PayloadAsyncLimitableCollectionGetOneOrMoreFromStart<T>(CancellationTokenSource, asyncLimitableCollection);
        }
        public PayloadAsyncLimitableCollectionGetOneOrMoreFromFinish<T> AsyncLimitableCollectionGetOneOrMoreFromFinish<T>(AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds)
        {
            return new PayloadAsyncLimitableCollectionGetOneOrMoreFromFinish<T>(CancellationTokenSource, asyncLimitableCollection, asyncLimitableCollectionOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload AsyncLimitableCollectionGetOneOrMoreFrom<T>(AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds, int maxBlocksToGet)
        {
            return FactoryBasic.Chain( 
                FactoryBasic.ReturnValue(maxBlocksToGet),
                AsyncLimitableCollectionGetOneOrMoreFromStart<T>(asyncLimitableCollection),
                AsyncLimitableCollectionGetOneOrMoreFromFinish<T>(asyncLimitableCollection, asyncLimitableCollectionOperationTimeoutMilliseconds)
            );
        }
    }
}