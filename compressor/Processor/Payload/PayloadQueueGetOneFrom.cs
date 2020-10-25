using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueGetOneFrom<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueGetOneFrom(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, queue, queueOperationTimeoutMilliseconds)
        {
        }

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