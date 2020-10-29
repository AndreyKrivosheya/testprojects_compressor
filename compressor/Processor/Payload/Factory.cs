using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class Factory
    {
        public Factory(CancellationTokenSource cancellationTokenSource)
        {
            this.CancellationTokenSource = cancellationTokenSource;
            this.FactoryBasic = new Common.Payload.Basic.Factory(this.CancellationTokenSource);
            this.FactoryCollections = new Common.Payload.Collections.Factory(this.CancellationTokenSource);
        }

        readonly CancellationTokenSource CancellationTokenSource;

        readonly Common.Payload.Basic.Factory FactoryBasic;
        readonly Common.Payload.Collections.Factory FactoryCollections;

        public Common.Payload.Payload QueueAddToQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return FactoryCollections.AsyncLimitableCollectionAddTo<BlockToProcess>(queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueAddToQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return FactoryCollections.AsyncLimitableCollectionAddTo<BlockToWrite>(queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueCompleteAddingQueueToProcess(QueueToProcess queue)
        {
            return FactoryCollections.AsyncLimitableCollectionCompleteAdding<BlockToProcess>(queue);
        }
        public Common.Payload.Payload QueueCompleteAddingQueueToWrite(QueueToWrite queue)
        {
            return FactoryCollections.AsyncLimitableCollectionCompleteAdding<BlockToWrite>(queue);
        }

        public Common.Payload.Payload QueueGetOneFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return FactoryCollections.AsyncLimitableCollectionGetOneFrom<BlockToProcess>(queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueGetOneFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return FactoryCollections.AsyncLimitableCollectionGetOneFrom<BlockToWrite>(queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds, int maxBlocksToGet)
        {
            return FactoryCollections.AsyncLimitableCollectionGetOneOrMoreFrom<BlockToProcess>(queue, queueOperationTimeoutMilliseconds, maxBlocksToGet);
        }
        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds, int maxBlocksToGet)
        {
            return FactoryCollections.AsyncLimitableCollectionGetOneOrMoreFrom<BlockToWrite>(queue, queueOperationTimeoutMilliseconds, maxBlocksToGet);
        }
        
        public PayloadProcessCompress ProcessCompress()
        {
            return new PayloadProcessCompress(CancellationTokenSource);
        }

        public PayloadProcessDecompress ProcessDecompress()
        {
            return new PayloadProcessDecompress(CancellationTokenSource);
        }

        public Common.Payload.Payload BytesToBlockToProcessBinary()
        {
            return new PayloadBytesToBlockToProcessBinary(CancellationTokenSource);
        }

        public PayloadBytesToBlockToProcessArchive BytesToBlockToProcessArchive()
        {
            return new PayloadBytesToBlockToProcessArchive(CancellationTokenSource);
        }

        public PayloadBlocksToWriteToBytesArchive BlocksToWriteToBytesArchive()
        {
            return new PayloadBlocksToWriteToBytesArchive(CancellationTokenSource);
        }

        public PayloadBlocksToWriteToBytesBinary BlocksToWriteToBytesBinary()
        {
            return new PayloadBlocksToWriteToBytesBinary(CancellationTokenSource);
        }

        public PayloadBlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteStart BlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteStart()
        {
            return new PayloadBlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteStart(CancellationTokenSource);
        }
        public PayloadBlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteFinish BlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteFinish(int waitTimeout)
        {
            return new PayloadBlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteFinish(CancellationTokenSource, waitTimeout);
        }
        public Common.Payload.Payload BlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWrite(int waitTimeout)
        {
            return FactoryBasic.Chain(
                BlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteStart(),
                BlockToWriteWaitPreviousBlockProcessedAndAddedToQueueToWriteFinish(waitTimeout)
            );
        }
    }
}