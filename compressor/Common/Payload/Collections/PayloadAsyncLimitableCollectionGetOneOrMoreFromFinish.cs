using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common;
using compressor.Common.Collections;
using compressor.Common.Payload;

namespace compressor.Common.Payload.Collections
{
    class PayloadAsyncLimitableCollectionGetOneOrMoreFromFinish<T>: PayloadAsyncLimitableCollection<T>
    {
        public PayloadAsyncLimitableCollectionGetOneOrMoreFromFinish(CancellationTokenSource cancellationTokenSource, AsyncLimitableCollection<T> asyncLimitableCollection, int asyncLimitableCollectionOperationTimeoutMilliseconds)
            : base(cancellationTokenSource, asyncLimitableCollection)
        {
            this.Timeout = asyncLimitableCollectionOperationTimeoutMilliseconds;
        }

        readonly int Timeout;
        
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((IAsyncResult takingAsyncResult) =>
                takingAsyncResult.WaitCompletedAndRunUnsafe(Timeout, CancellationTokenSource.Token,
                    whenCompleted: (completedTakingAsyncResult) =>
                    {
                        var maxBlocksToGet = (int)completedTakingAsyncResult.AsyncState;
                        var blocksFromQueue = new List<T>(Math.Min(1, maxBlocksToGet));
                        while(blocksFromQueue.Count < blocksFromQueue.Capacity)
                        {
                            if(blocksFromQueue.Count == 0 )
                            {
                                try
                                {
                                    var blockTaken = AsyncLimitableCollection.EndTake(completedTakingAsyncResult);
                                    blocksFromQueue.Add(blockTaken);
                                    continue;
                                }
                                catch(InvalidOperationException)
                                {
                                    if(AsyncLimitableCollection.IsCompleted)
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
                                if(AsyncLimitableCollection.Count < 1)
                                {
                                    break;
                                }
                                else
                                {
                                    using(var cancellationForGettingMoreTheOneItemIfNotCompletedSynchroniously = new CancellationTokenSource())
                                    {
                                        using(var cancellationCombined = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token, cancellationForGettingMoreTheOneItemIfNotCompletedSynchroniously.Token))
                                        {
                                            // try taking item expecting it be already available
                                            IAsyncResult asyncResultGettingMoreThenOneItem;
                                            {
                                                try
                                                {
                                                    asyncResultGettingMoreThenOneItem = AsyncLimitableCollection.BeginTake(cancellationCombined.Token);
                                                }
                                                catch(InvalidOperationException)
                                                {
                                                    if(AsyncLimitableCollection.IsCompleted)
                                                    {
                                                        break;
                                                    }

                                                    throw;
                                                }
                                            }
                                            // if item is not available (and taking didn't completed in BeginTake) cancel taking
                                            if(!asyncResultGettingMoreThenOneItem.IsCompleted)
                                            {
                                                cancellationForGettingMoreTheOneItemIfNotCompletedSynchroniously.Cancel();
                                            }
                                            // end taking item to either get item or canceled execption
                                            {
                                                try
                                                {
                                                    var blockTaken = AsyncLimitableCollection.EndTake(asyncResultGettingMoreThenOneItem);
                                                    blocksFromQueue.Add(blockTaken);
                                                    continue;
                                                }
                                                catch(OperationCanceledException)
                                                {
                                                    if(cancellationForGettingMoreTheOneItemIfNotCompletedSynchroniously.IsCancellationRequested)
                                                    {
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        throw;
                                                    }
                                                }
                                                catch(InvalidOperationException)
                                                {
                                                    if(AsyncLimitableCollection.IsCompleted)
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
                            if(AsyncLimitableCollection.IsCompleted)
                            {
                                return new PayloadResultSucceeded();
                            }

                            return new PayloadResultContinuationPendingDoneNothing();
                        }
                    }
                )
            );
        }
    }
}