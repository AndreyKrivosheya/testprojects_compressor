using System;
using System.Threading;

namespace compressor
{
    abstract class ProcessorQueueBlock
    {
        protected class AwaiterForAddedToQueue
        {
            public AwaiterForAddedToQueue(AwaiterForAddedToQueue previous)
            {
                this.Previous = previous;
            }
            public AwaiterForAddedToQueue()
                : this(null)
            {
            }

            protected AwaiterForAddedToQueue Previous { get; private set; }

            readonly object BlockAddedToQueueLazyLock = new object();
            Lazy<ManualResetEvent> BlockAddedToQueueLazy = new Lazy<ManualResetEvent>(() => new ManualResetEvent(false));
            public void NotifyAddedToQueue()
            {
                lock(BlockAddedToQueueLazyLock)
                {
                    if(BlockAddedToQueueLazy.IsValueCreated)
                    {
                        if(BlockAddedToQueueLazy.Value != null)
                        {
                            BlockAddedToQueueLazy.Value.Set();
                        }
                    }
                    BlockAddedToQueueLazy = new Lazy<ManualResetEvent>(() => null);
                }
            }
            public bool WaitAllPreviousBlocksAddedToQueue(int milliseconds, CancellationToken cancellationToken)
            {
                if(null != Previous)
                {
                    if(!Previous.WaitThisAndAllPreviousBlocksAddedToQueue(milliseconds, cancellationToken))
                    {
                        return false;
                    }
                    else
                    {
                        // wait finished, next time should wait for no previous block
                        Previous = null;
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            public bool WaitAllPreviousBlocksAddedToQueue(CancellationToken cancellationToken)
            {
                return WaitAllPreviousBlocksAddedToQueue(0, cancellationToken);
            }
            public bool WaitThisAndAllPreviousBlocksAddedToQueue(int milliseconds, CancellationToken cancellationToken)
            {
                // wait all previous blocks were added to queue
                if(!WaitAllPreviousBlocksAddedToQueue(milliseconds, cancellationToken))
                {
                    return false;
                }
                // wait this block to be added to queue
                WaitHandle waitableThisBlockAddedToQueue;
                lock(BlockAddedToQueueLazyLock)
                {
                    waitableThisBlockAddedToQueue = BlockAddedToQueueLazy.Value;
                }
                if(waitableThisBlockAddedToQueue != null)
                {
                    if(0 == WaitHandle.WaitAny(new [] { waitableThisBlockAddedToQueue, cancellationToken.WaitHandle }, milliseconds))
                    {
                        lock(BlockAddedToQueueLazyLock)
                        {
                            BlockAddedToQueueLazy = new Lazy<ManualResetEvent>(() => null);
                        }
                        
                        try
                        {
                            waitableThisBlockAddedToQueue.Close();
                        }
                        catch(Exception)
                        {
                            // probably already closed on another thread
                            // one of the threads should succeed
                        }
                        
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            public bool WaitThisAndAllPreviousBlocksAddedToQueue(CancellationToken cancellationToken)
            {
                return WaitThisAndAllPreviousBlocksAddedToQueue(0, cancellationToken);
            }
        };

        protected ProcessorQueueBlock(AwaiterForAddedToQueue awaiter, long originalLength, byte[] data)
        {
            this.Awaiter = awaiter;
            this.OriginalLength = originalLength;
            this.Data = data;
        }
        protected ProcessorQueueBlock(ProcessorQueueBlock awaiterBlock, long length, byte[] data)
            : this(awaiterBlock.Awaiter, length, data)
        {
        }

        protected readonly AwaiterForAddedToQueue Awaiter;
        public readonly byte[] Data;
        public long OriginalLength;
    }
}