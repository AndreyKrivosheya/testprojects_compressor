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
        }

        readonly CancellationTokenSource CancellationTokenSource;

        readonly Common.Payload.Basic.Factory FactoryBasic;

        public PayloadQueueAddToQueueToProcess QueueAddToQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddToQueueToProcess(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }

        public PayloadQueueAddToQueueToWrite QueueAddToQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddToQueueToWrite(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }

        public PayloadQueueGetOneFrom<BlockToProcess> QueueGetOneFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFrom<BlockToProcess>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        
        public PayloadQueueGetOneFrom<BlockToWrite> QueueGetOneFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFrom<BlockToWrite>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }

        public PayloadQueueGetOneOrMoreFrom<BlockToProcess> QueueGetOneOrMoreFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneOrMoreFrom<BlockToProcess>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds, int maxBlocksToGet)
        {
            return FactoryBasic.Chain(
                FactoryBasic.ReturnValue(maxBlocksToGet),
                QueueGetOneFromQueueToProcess(queue, queueOperationTimeoutMilliseconds)
            );
        }

        public PayloadQueueGetOneOrMoreFrom<BlockToWrite> QueueGetOneOrMoreFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneOrMoreFrom<BlockToWrite>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds, int maxBlocksToGet)
        {
            return FactoryBasic.Chain(
                FactoryBasic.ReturnValue(maxBlocksToGet),
                QueueGetOneOrMoreFromQueueToWrite(queue, queueOperationTimeoutMilliseconds)
            );
        }

        public PayloadQueueCompleteAdding<BlockToProcess> QueueCompleteAddingQueueToProcess(QueueToProcess queue)
        {
            return new PayloadQueueCompleteAdding<BlockToProcess>(CancellationTokenSource, queue);
        }

        public PayloadQueueCompleteAdding<BlockToWrite> QueueCompleteAddingQueueToWrite(QueueToWrite queue)
        {
            return new PayloadQueueCompleteAdding<BlockToWrite>(CancellationTokenSource, queue);
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
    }
}