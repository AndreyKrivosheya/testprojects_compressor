using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadQueueAddTo<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueAddTo(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, queue, queueOperationTimeoutMilliseconds)
        {
        }

        protected virtual PayloadResult RunUnsafe(TBlock blockToAdd)
        {
            try
            {
                if(Queue.TryAdd(blockToAdd, Timeout, CancellationTokenSource.Token))
                {
                    return new PayloadResultContinuationPending(blockToAdd);
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
            catch(OperationCanceledException)
            {
                return new PayloadResultCanceled();
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
            return parameter.VerifyNotNullConvertAndRunUnsafe<TBlock>(RunUnsafe);
        }
    }
}