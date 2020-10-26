using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueGetOneOrMoreFrom<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueGetOneOrMoreFrom(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue, int queueOperationTimeoutMillisecondsTimeout)
            : base(cancellationTokenSource, queue, queueOperationTimeoutMillisecondsTimeout)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (int maxBlocksToGet) =>
            {
                var blocksFromQueue = new List<TBlock>(maxBlocksToGet < 1 ? Queue.MaxCapacity : maxBlocksToGet);
                while(blocksFromQueue.Count < blocksFromQueue.Capacity)
                {
                    TBlock blockFromQueue;
                    bool taken = false;
                    try
                    {
                        taken = Queue.TryTake(out blockFromQueue, Timeout, CancellationTokenSource.Token);
                    }
                    catch(OperationCanceledException)
                    {
                        return new PayloadResultCanceled();
                    }
                    catch(InvalidOperationException)
                    {
                        if(Queue.IsCompleted)
                        {
                            break;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if(taken)
                    {
                        blocksFromQueue.Add(blockFromQueue);
                    }
                    else
                    {
                        break;
                    }
                }

                if(blocksFromQueue.Count > 0)
                {
                    return new PayloadResultContinuationPending(blocksFromQueue);
                }
                else
                {
                    if(Queue.IsCompleted)
                    {
                        return new PayloadResultSucceeded();
                    }

                    return new PayloadResultContinuationPendingDoneNothing();
                }
            });
        }
    }
}