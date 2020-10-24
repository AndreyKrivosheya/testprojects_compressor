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

        public void NotifyAddedToQueue()
        {
            Awaiter.NotifyAddedToQueue();
        }

        public bool WaitAllPreviousBlocksAddedToQueue(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Awaiter.WaitAllPreviousBlocksAddedToQueue(millisecondsTimeout, cancellationToken);
        }
        public bool WaitAllPreviousBlocksAddedToQueue(CancellationToken cancellationToken)
        {
            return Awaiter.WaitAllPreviousBlocksAddedToQueue(cancellationToken);
        }

        public bool WaitThisAndAllPreviousBlocksAddedToQueue(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Awaiter.WaitThisAndAllPreviousBlocksAddedToQueue(millisecondsTimeout, cancellationToken);
        }
        public bool WaitThisAndAllPreviousBlocksAddedToQueue(CancellationToken cancellationToken)
        {
            return Awaiter.WaitThisAndAllPreviousBlocksAddedToQueue(cancellationToken);
        }
    }
}