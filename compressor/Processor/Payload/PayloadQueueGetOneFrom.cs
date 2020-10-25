using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueGetOneFrom<TBlock>: Payload
        where TBlock: Block
    {
        public PayloadQueueGetOneFrom(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, settings)
        {
            this.Queue = queue;
            this.Timeout = queueOperationTimeoutMilliseconds;
        }

        readonly Queue.Queue<TBlock> Queue;
        readonly int Timeout;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            TBlock blockFromQueue = null;
            bool taken = false;
            try
            {
                taken = Queue.TryTake(out blockFromQueue, Timeout, CancellationTokenSource.Token);
            }
            catch(InvalidOperationException)
            {
                if(!Queue.IsCompleted)
                {
                    throw;
                }
            }

            if(taken)
            {
                return new PayloadResultContinuationPending(blockFromQueue);
            }
            else
            {
                if(Queue.IsCompleted)
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
}