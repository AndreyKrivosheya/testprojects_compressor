using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
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

        public Payload CreateQueueAddToQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddTo<BlockToProcess>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Payload CreateQueueAddToQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddTo<BlockToWrite>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Payload CreateQueueGetOneFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFrom<BlockToProcess>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }
        
        public Payload CreateQueueGetOneFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFrom<BlockToWrite>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Payload CreateGetOneOrMoreFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneOrMoreFrom<BlockToProcess>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Payload CreateGetOneOrMoreFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneOrMoreFrom<BlockToWrite>(CancellationTokenSource, Settings, queue, queueOperationTimeoutMilliseconds);
        }

        public Payload CreateProcessCompress()
        {
            return new PayloadProcessCompress(CancellationTokenSource, Settings);
        }

        public Payload CreateProcessDecompress()
        {
            return new PayloadProcessDecompress(CancellationTokenSource, Settings);
        }

        public Payload CreateReadBlockFromBinary(Stream stream, QueueToProcess queue)
        {
            return new PayloadReadBlockFromBinary(CancellationTokenSource, Settings, stream, queue);
        }

        public Payload CreateReadBlockFromBinaryFinish(Stream stream, QueueToProcess queue)
        {
            return new PayloadReadBlockFromBinaryFinish(CancellationTokenSource, Settings, stream, queue);
        }

        public Payload CreateBlocksToWriteToBytesArchive()
        {
            return new PayloadBlocksToWriteToBytesArchive(CancellationTokenSource, Settings);
        }

        public Payload CreateCompleteProcessing(QueueToWrite queue)
        {
            return new PayloadCompleteProcessing(CancellationTokenSource, Settings, queue);
        }

        public Payload CreateCompleteWriting(Stream stream)
        {
            return new PayloadCompleteWriting(CancellationTokenSource, Settings, stream);
        }
   }
}