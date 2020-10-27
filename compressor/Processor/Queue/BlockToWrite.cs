using System;
using System.Threading;

namespace compressor.Processor.Queue
{
    class BlockToWrite: Block
    {
        public BlockToWrite(BlockToProcess originalBlock, byte[] data)
            : base(originalBlock, originalBlock.OriginalLength, data)
        {
        }

        public bool Last
        {
            get
            {
                return Awaiter.Last;
            }
        }

        public void NotifyProcessedAndAddedToQueue()
        {
            Awaiter.NotifyProcessedAndAddedToQueueToWrite();
        }

        public bool WaitAllPreviousBlocksProcessedAndAddedToQueue(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Awaiter.WaitAllPreviousBlocksProcessedAddedToQueueToWrite(millisecondsTimeout, cancellationToken);
        }
        public bool WaitAllPreviousBlocksProcessedAndAddedToQueue(CancellationToken cancellationToken)
        {
            return Awaiter.WaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(cancellationToken);
        }

        public bool WaitThisAndAllPreviousBlocksProcessedAndAddedToQueue(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Awaiter.WaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(millisecondsTimeout, cancellationToken);
        }
        public bool WaitThisAndAllPreviousBlocksProcessedAndAddedToQueue(CancellationToken cancellationToken)
        {
            return Awaiter.WaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(cancellationToken);
        }
    }
}