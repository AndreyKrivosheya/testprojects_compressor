using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueGetOneOrMoreFromStart<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueGetOneOrMoreFromStart(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue)
            : base(cancellationTokenSource, queue)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (int maxBlocksToGet) =>
            {
                try
                {
                    var takingAyncResult = Queue.BeginTake(CancellationTokenSource.Token, state: maxBlocksToGet);
                    return new PayloadResultContinuationPending(takingAyncResult);
                }
                catch(OperationCanceledException)
                {
                    return new PayloadResultCanceled();
                }
                catch(InvalidOperationException)
                {
                    if(Queue.IsCompleted)
                    {
                        return new PayloadResultContinuationPendingDoneNothing();
                    }

                    throw;
                }
            });
        }
    }
}