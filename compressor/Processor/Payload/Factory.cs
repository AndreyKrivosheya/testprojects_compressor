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

        public Common.Payload.Payload QueueAddToQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddToQueueToProcess(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueAddToQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddToQueueToWrite(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueGetOneFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFrom<BlockToProcess>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        
        public Common.Payload.Payload QueueGetOneFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFrom<BlockToWrite>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
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

        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
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

        public Common.Payload.Payload QueueCompleteAddingQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueCompleteAdding<BlockToProcess>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueCompleteAddingQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueCompleteAdding<BlockToWrite>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload ProcessCompress()
        {
            return new PayloadProcessCompress(CancellationTokenSource);
        }

        public Common.Payload.Payload ProcessDecompress()
        {
            return new PayloadProcessDecompress(CancellationTokenSource);
        }

        public Common.Payload.Payload BytesToBlockToProcessBinary()
        {
            return new PayloadBytesToBlockToProcessBinary(CancellationTokenSource);
        }

        public Common.Payload.Payload BytesToBlockToProcessArchive()
        {
            return new PayloadBytesToBlockToProcessArchive(CancellationTokenSource);
        }

        public Common.Payload.Payload BlocksToWriteToBytesArchive()
        {
            return new PayloadBlocksToWriteToBytesArchive(CancellationTokenSource);
        }

        public Common.Payload.Payload BlocksToWriteToBytesBinary()
        {
            return new PayloadBlocksToWriteToBytesBinary(CancellationTokenSource);
        }
    }
}