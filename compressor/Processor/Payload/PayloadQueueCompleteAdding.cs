using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    static class PayloadQueueCompleteAdding
    {
        public static readonly object LastObjectAdded = new object();
    }

    class PayloadQueueCompleteAdding<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueCompleteAdding(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue)
            : base(cancellationTokenSource, queue)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            Queue.CompleteAdding();
            return new PayloadResultSucceeded();
        }
    }
}