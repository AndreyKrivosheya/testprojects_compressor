using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueAddToStart<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueAddToStart(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue)
            : base(cancellationTokenSource, queue)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (TBlock blockToAdd) => 
            {
                try
                {
                    var addingAyncResult = Queue.BeginAdd(blockToAdd, CancellationTokenSource.Token);
                    return new PayloadResultContinuationPending(addingAyncResult);
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
            });
        }
    }
}