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

        public void NotifyAddedToQueue()
        {
            Awaiter.NotifyAddedToQueue();
        }
        public bool WaitAllPreviousBlocksAddedToQueue(int milliseconds, CancellationToken cancellationToken)
        {
            return Awaiter.WaitAllPreviousBlocksAddedToQueue(milliseconds, cancellationToken);
        }
        public bool WaitAllPreviousBlocksAddedToQueue(CancellationToken cancellationToken)
        {
            return Awaiter.WaitAllPreviousBlocksAddedToQueue(cancellationToken);
        }
        public bool WaitThisAndAllPreviousBlocksAddedToQueue(int milliseconds, CancellationToken cancellationToken)
        {
            return Awaiter.WaitThisAndAllPreviousBlocksAddedToQueue(milliseconds, cancellationToken);
        }
        public bool WaitThisAndAllPreviousBlocksAddedToQueue(CancellationToken cancellationToken)
        {
            return Awaiter.WaitThisAndAllPreviousBlocksAddedToQueue(cancellationToken);
        }
    }
}