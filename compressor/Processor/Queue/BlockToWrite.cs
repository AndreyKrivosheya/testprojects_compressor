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

        public IAsyncResult BeginWaitPreviousBlockProcessedAndAddedToQueue(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            return Awaiter.BeginWaitPreviousBlockProcessedAndAddedToQueueToWrite(cancellationToken, asyncCallback, state);
        }
        public IAsyncResult BeginWaitPreviousBlockProcessedAndAddedToQueue(AsyncCallback asyncCallback = null, object state = null)
        {
            return Awaiter.BeginWaitPreviousBlockProcessedAndAddedToQueueToWrite(asyncCallback, state);
        }

        public void EndWaitPreviousBlockProcessedAndAddedToQueueToWrite(IAsyncResult waitingAsyncResult)
        {
            Awaiter.EndWaitPreviousBlockProcessedAndAddedToQueueToWrite(waitingAsyncResult);
        }
    }
}