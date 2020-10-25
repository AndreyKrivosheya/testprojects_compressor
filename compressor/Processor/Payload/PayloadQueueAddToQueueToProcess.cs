using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueAddToQueueToProcess: PayloadQueueAddTo<BlockToProcess>
    {
        public PayloadQueueAddToQueueToProcess(CancellationTokenSource cancellationTokenSource, QueueToProcess queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, queue, queueOperationTimeoutMilliseconds)
        {
        }
    }
}