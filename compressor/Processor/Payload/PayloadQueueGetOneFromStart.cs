using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueGetOneFromStart<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueGetOneFromStart(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue)
            : base(cancellationTokenSource, queue)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            try
            {
                var takingAyncResult = Queue.BeginTake(CancellationTokenSource.Token);
                return new PayloadResultContinuationPending(takingAyncResult);
            }
            catch(OperationCanceledException)
            {
                return new PayloadResultCanceled();
            }
            catch(InvalidOperationException)
            {
                if(!Queue.IsCompleted)
                {
                    throw;
                }
                else
                {
                    return new PayloadResultContinuationPendingDoneNothing();
                }
            }
        }
    }
}