using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class Factory
    {
        public Factory(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
        {
            this.CancellationTokenSource = cancellationTokenSource;
            this.Settings = settings;
        }

        readonly CancellationTokenSource CancellationTokenSource;
        readonly SettingsProvider Settings;

        public Common.Payload.Payload QueueAddToQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddTo<BlockToProcess>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueAddToQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddToQueueToWrite(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueGetOneFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFrom<BlockToProcess>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }
        
        public Common.Payload.Payload QueueGetOneFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFrom<BlockToWrite>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneOrMoreFrom<BlockToProcess>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneOrMoreFrom<BlockToWrite>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueCompleteAddingQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueCompleteAdding<BlockToProcess>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload QueueCompleteAddingQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueCompleteAdding<BlockToWrite>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Common.Payload.Payload ProcessCompress()
        {
            return new PayloadProcessCompress(CancellationTokenSource, Settings);
        }

        public Common.Payload.Payload ProcessDecompress()
        {
            return new PayloadProcessDecompress(CancellationTokenSource, Settings);
        }

        public Common.Payload.Payload BytesToBlockToProcessBinary()
        {
            return new PayloadBytesToBlockToProcessBinary(CancellationTokenSource, Settings);
        }

        public Common.Payload.Payload BytesToBlockToProcessArchive()
        {
            return new PayloadBytesToBlockToProcessArchive(CancellationTokenSource, Settings);
        }

        public Common.Payload.Payload BlocksToWriteToBytesArchive()
        {
            return new PayloadBlocksToWriteToBytesArchive(CancellationTokenSource, Settings);
        }

        public Common.Payload.Payload BlocksToWriteToBytesBinary()
        {
            return new PayloadBlocksToWriteToBytesBinary(CancellationTokenSource, Settings);
        }

        public Common.Payload.Payload CompleteWriting(Stream stream)
        {
            return new PayloadCompleteWriting(CancellationTokenSource, Settings, stream);
        }
   }
}