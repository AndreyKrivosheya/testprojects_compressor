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
        public PayloadQueueGetOneOrMoreFrom(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue, int queueOperationTimeoutMillisecondsTimeout, int maxBlocksToGet)
            : base(cancellationTokenSource, queue, queueOperationTimeoutMillisecondsTimeout)
        {
            // get as much blocks as possible
            if(maxBlocksToGet < 1)
            {
                this.MaxBlocksToGet = -1;
            }
            else
            {
                this.MaxBlocksToGet = maxBlocksToGet;
            }
        }
        public PayloadQueueGetOneOrMoreFrom(CancellationTokenSource cancellationTokenSource, Queue.Queue<TBlock> queue, int queueOperationTimeoutMillisecondsTimeout)
            : this(cancellationTokenSource, queue, queueOperationTimeoutMillisecondsTimeout, -1)
        {
        }

        readonly int MaxBlocksToGet;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            var blocksFromQueue = new List<TBlock>(MaxBlocksToGet == -1 ? Queue.MaxCapacity : MaxBlocksToGet);
            while(blocksFromQueue.Count < blocksFromQueue.Capacity)
            {
                TBlock blockFromQueue;
                bool taken = false;
                try
                {
                    taken = Queue.TryTake(out blockFromQueue, Timeout, CancellationTokenSource.Token);
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
        }
    }
}