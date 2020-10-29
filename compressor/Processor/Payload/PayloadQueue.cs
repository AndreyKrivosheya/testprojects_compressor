using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadQueue<TBlock>: Common.Payload.Payload
        where TBlock: Block
    {
        public PayloadQueue(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue)
            : base(cancellationTokenSource)
        {
            this.Queue = queue;
        }

        protected readonly Queue.Queue<TBlock> Queue;
    }
}