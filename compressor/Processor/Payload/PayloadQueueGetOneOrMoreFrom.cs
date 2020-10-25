using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadQueueGetOneOrMoreFrom<TBlock>: Payload
        where TBlock: Block
    {
        public PayloadQueueGetOneOrMoreFrom(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Queue.Queue<TBlock> queue, int queueOperationTimeoutMillisecondsTimeout)
            : base(cancellationTokenSource, settings)
        {
            this.Queue = queue;
            this.Timeout = queueOperationTimeoutMillisecondsTimeout;
        }

        readonly Queue.Queue<TBlock> Queue;
        readonly int Timeout;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            var blocksFromQueue = new List<TBlock>(Settings.MaxBlocksToWriteAtOnce);
            while(blocksFromQueue.Count < blocksFromQueue.Capacity)
            {
                TBlock blockFromQueue;
                bool taken = false;
                try
                {
                    taken = Queue.TryTake(out blockFromQueue);
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