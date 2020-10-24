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
        public PayloadQueueCompleteAdding(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, settings, queue, queueOperationTimeoutMilliseconds)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            if(Queue.CompleteAdding(Timeout, CancellationTokenSource.Token))
            {
                return new PayloadResultSucceeded();
            }
            else
            {
                return new PayloadResultContinuationPendingDoneNothing();
            }
        }
    }
}