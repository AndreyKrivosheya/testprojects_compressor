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

        public IAsyncResult BeginWaitAllPreviousBlocksProcessedAndAddedToQueue(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            return Awaiter.BeginWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(cancellationToken, asyncCallback, state);
        }
        public IAsyncResult BeginWaitAllPreviousBlocksProcessedAndAddedToQueue(AsyncCallback asyncCallback = null, object state = null)
        {
            return Awaiter.BeginWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(asyncCallback, state);
        }

        public void EndWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(IAsyncResult waitingAsyncResult)
        {
            Awaiter.EndWaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(waitingAsyncResult);
        }
    }
}