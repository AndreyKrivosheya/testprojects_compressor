using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueAddTo<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueAddTo(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, settings, queue, queueOperationTimeoutMilliseconds)
        {
        }

        protected virtual PayloadResult RunUnsafe(TBlock blockToAdd)
        {
            try
            {
                if(Queue.TryAdd(blockToAdd, Timeout, CancellationTokenSource.Token))
                {
                    return new PayloadResultContinuationPending();
                }
                else
                {
                    if(Queue.IsAddingCompleted)
                    {
                        // something wrong: queue is closed for additions, but there's block outstanding
                        // probably there's an exception on another worker thread
                        return new PayloadResultSucceeded();
                    }
                    else
                    {
                        return new PayloadResultContinuationPendingDoneNothing();
                    }
                }
            }
            catch(InvalidOperationException)
            {
                if(Queue.IsAddingCompleted)
                {
                    // something wrong: queue is closed for additions, but there's block outstanding
                    // probably there's an exception on another worker thread
                    return new PayloadResultSucceeded();
                }
                else
                {
                    throw;
                }
            }
        }
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return VerifyParameterNotNullConvertAndRunUnsafe(parameter,
            ((Func<TBlock, PayloadResult>)RunUnsafe));
        }
    }
}