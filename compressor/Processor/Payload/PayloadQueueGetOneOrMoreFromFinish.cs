using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Processor.Queue;

namespace compressor.Processor.Payload
{
    class PayloadQueueGetOneOrMoreFromFinish<TBlock>: PayloadQueue<TBlock>
        where TBlock: Block
    {
        public PayloadQueueGetOneOrMoreFromFinish(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue, int queueOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, queue)
        {
            this.Timeout = queueOperationTimeoutMilliseconds;
        }

        readonly int Timeout;
        
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (IAsyncResult takingAsyncResult) =>
            {
                return takingAsyncResult.WaitCompleted<PayloadResult>(Timeout, CancellationTokenSource.Token,
                    whileWaitTimedOut:
                        (incompleteAsyncResult) => new PayloadResultContinuationPendingDoneNothing(),
                    whenCompleted:
                        (completedAsyncResult) =>
                        {
                            var maxBlocksToGet = (int)completedAsyncResult.AsyncState;
                            var blocksFromQueue = new List<TBlock>(Math.Min(1, maxBlocksToGet));
                            while(blocksFromQueue.Count < blocksFromQueue.Capacity)
                            {
                                if(blocksFromQueue.Count == 0 )
                                {
                                    try
                                    {
                                        var blockTaken = Queue.EndTake(completedAsyncResult);
                                        blocksFromQueue.Add(blockTaken);
                                        continue;
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
                                }
                                else
                                {
                                    if(Queue.Count < 1)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        using(var cancellationForTakeIfNotCompletedSynchroniously = new CancellationTokenSource())
                                        {
                                            using(var cancellationCombined = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token, cancellationForTakeIfNotCompletedSynchroniously.Token))
                                            {
                                                // try taking item expecting it be already available
                                                IAsyncResult asyncResultGettingMoreThenOneItem;
                                                {
                                                    try
                                                    {
                                                        asyncResultGettingMoreThenOneItem = Queue.BeginTake(cancellationCombined.Token);
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

                                                        throw;
                                                    }
                                                }
                                                // if item is not available (and taking didn't completed in BeginTake) cancel taking
                                                if(!asyncResultGettingMoreThenOneItem.IsCompleted)
                                                {
                                                    cancellationForTakeIfNotCompletedSynchroniously.Cancel();
                                                }
                                                // end taking item to either get item or canceled execption
                                                {
                                                    try
                                                    {
                                                        var blockTaken = Queue.EndTake(asyncResultGettingMoreThenOneItem);
                                                        blocksFromQueue.Add(blockTaken);
                                                        continue;
                                                    }
                                                    catch(OperationCanceledException)
                                                    {
                                                        if(cancellationForTakeIfNotCompletedSynchroniously.IsCancellationRequested)
                                                        {
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            return new PayloadResultCanceled();
                                                        }
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
                                                }
                                            }
                                        }
                                    }
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
                        }
                );
            });
        }
    }
}