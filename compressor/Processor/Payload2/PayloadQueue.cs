using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    abstract class PayloadQueue<TBlock>: Payload
        where TBlock: Block
    {
        public PayloadQueue(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, settings)
        {
            this.Queue = queue;
            this.Timeout = queueOperationTimeoutMilliseconds;
        }

        protected readonly Queue.Queue<TBlock> Queue;
        protected readonly int Timeout;
    }
}