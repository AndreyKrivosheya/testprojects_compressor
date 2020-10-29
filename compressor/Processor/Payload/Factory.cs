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

        public PayloadQueueAddToStart<BlockToProcess> QueueAddToQueueToProcessStart(QueueToProcess queue)
        {
            return new PayloadQueueAddToStart<BlockToProcess>(CancellationTokenSource, queue);
        }
        public PayloadQueueAddToFinish<BlockToProcess> QueueAddToQueueToProcessFinish(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddToFinish<BlockToProcess>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueAddToQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain(
                QueueAddToQueueToProcessStart(queue),
                QueueAddToQueueToProcessFinish(queue, queueOperationTimeoutMilliseconds)
            );
        }

        public PayloadQueueAddToStart<BlockToWrite> QueueAddToQueueToWriteStart(QueueToWrite queue)
        {
            return new PayloadQueueAddToStart<BlockToWrite>(CancellationTokenSource, queue);
        }
        public PayloadQueueAddToFinish<BlockToWrite> QueueAddToQueueToWriteFinish(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueAddToFinish<BlockToWrite>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueAddToQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain(
                QueueAddToQueueToWriteStart(queue),
                QueueAddToQueueToWriteFinish(queue, queueOperationTimeoutMilliseconds)
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

        public PayloadQueueGetOneFromStart<BlockToProcess> QueueGetOneFromQueueToProcessStart(QueueToProcess queue)
        {
            return new PayloadQueueGetOneFromStart<BlockToProcess>(CancellationTokenSource, queue);
        }
        public PayloadQueueGetOneFromFinish<BlockToProcess> QueueGetOneFromQueueToProcessFinish(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFromFinish<BlockToProcess>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueGetOneFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain( 
                QueueGetOneFromQueueToProcessStart(queue),
                QueueGetOneFromQueueToProcessFinish(queue, queueOperationTimeoutMilliseconds)
            );
        }
        
        public PayloadQueueGetOneFromStart<BlockToWrite> QueueGetOneFromQueueToWriteStart(QueueToWrite queue)
        {
            return new PayloadQueueGetOneFromStart<BlockToWrite>(CancellationTokenSource, queue);
        }
        public PayloadQueueGetOneFromFinish<BlockToWrite> QueueGetOneFromQueueToWriteFinish(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneFromFinish<BlockToWrite>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueGetOneFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain(
                QueueGetOneFromQueueToWriteStart(queue),
                QueueGetOneFromQueueToWriteFinish(queue, queueOperationTimeoutMilliseconds)
            );
        }

        public PayloadQueueGetOneOrMoreFromStart<BlockToProcess> QueueGetOneOrMoreFromQueueToProcessStart(QueueToProcess queue)
        {
            return new PayloadQueueGetOneOrMoreFromStart<BlockToProcess>(CancellationTokenSource, queue);
        }
        public PayloadQueueGetOneOrMoreFromFinish<BlockToProcess> QueueGetOneOrMoreFromQueueToProcessFinish(QueueToProcess queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneOrMoreFromFinish<BlockToProcess>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToProcess(QueueToProcess queue, int queueOperationTimeoutMilliseconds, int maxBlocksToGet)
        {
            return FactoryBasic.Chain( 
                FactoryBasic.ReturnValue(maxBlocksToGet),
                QueueGetOneOrMoreFromQueueToProcessStart(queue),
                QueueGetOneOrMoreFromQueueToProcessFinish(queue, queueOperationTimeoutMilliseconds)
            );
        }
        
        public PayloadQueueGetOneOrMoreFromStart<BlockToWrite> QueueGetOneOrMoreFromQueueToWriteStart(QueueToWrite queue)
        {
            return new PayloadQueueGetOneOrMoreFromStart<BlockToWrite>(CancellationTokenSource, queue);
        }
        public PayloadQueueGetOneOrMoreFromFinish<BlockToWrite> QueueGetOneOrMoreFromQueueToWriteFinish(QueueToWrite queue, int queueOperationTimeoutMilliseconds)
        {
            return new PayloadQueueGetOneOrMoreFromFinish<BlockToWrite>(CancellationTokenSource, queue, queueOperationTimeoutMilliseconds);
        }
        public Common.Payload.Payload QueueGetOneOrMoreFromQueueToWrite(QueueToWrite queue, int queueOperationTimeoutMilliseconds, int maxBlocksToGet)
        {
            return FactoryBasic.Chain(
                FactoryBasic.ReturnValue(maxBlocksToGet),
                QueueGetOneOrMoreFromQueueToWriteStart(queue),
                QueueGetOneOrMoreFromQueueToWriteFinish(queue, queueOperationTimeoutMilliseconds)
            );
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

        public PayloadBlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteStart BlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteStart()
        {
            return new PayloadBlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteStart(CancellationTokenSource);
        }
        public PayloadBlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteFinish BlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteFinish(int waitTimeout)
        {
            return new PayloadBlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteFinish(CancellationTokenSource, waitTimeout);
        }
        public Common.Payload.Payload BlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(int waitTimeout)
        {
            return FactoryBasic.Chain(
                BlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteStart(),
                BlockToWriteWaitAllPreviousBlocksProcessedAndAddedToQueueToWriteFinish(waitTimeout)
            );
        }
    }
}