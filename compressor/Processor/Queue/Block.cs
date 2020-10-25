using System;
using System.Threading;

namespace compressor.Processor.Queue
{
    abstract class Block
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
                    Previous = null;
                    BlockAddedToQueueLazy = new Lazy<ManualResetEvent>(() => null);
                }
            }
            public bool WaitAllPreviousBlocksAddedToQueue(int milliseconds, CancellationToken cancellationToken)
            {
                var previous = Previous;
                if(null != previous)
                {
                    if(!previous.WaitThisAndAllPreviousBlocksAddedToQueue(milliseconds, cancellationToken))
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
                    bool waitingEndedDueToAddedToQueue = false;
                    bool waitingEndedDueToWaitableDisposed = false;
                    try
                    {
                        if(0 == WaitHandle.WaitAny(new [] { waitableThisBlockAddedToQueue, cancellationToken.WaitHandle }, milliseconds))
                        {
                            waitingEndedDueToAddedToQueue = true;
                        }
                    }
                    catch(ObjectDisposedException)
                    {
                        // waitable for added to queue was closed before receiving signal on this thread
                        waitingEndedDueToAddedToQueue = true;
                        waitingEndedDueToWaitableDisposed = true;
                    }
                    if(waitingEndedDueToAddedToQueue)
                    {
                        if(!waitingEndedDueToWaitableDisposed)
                        {
                            // reset waitable generator, so that future waits will wait nothing
                            lock(BlockAddedToQueueLazyLock)
                            {
                                BlockAddedToQueueLazy = new Lazy<ManualResetEvent>(() => null);
                            }
                            // close waitable
                            try
                            {
                                waitableThisBlockAddedToQueue.Close();
                            }
                            catch(Exception)
                            {
                                // probably already closed on another thread
                                // one of the threads should succeed
                            }
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

        protected Block(AwaiterForAddedToQueue awaiter, long originalLength, byte[] data)
        {
            this.Awaiter = awaiter;
            this.OriginalLength = originalLength;
            this.Data = data;
        }
        protected Block(Block awaiterBlock, long originalLength, byte[] data)
            : this(awaiterBlock.Awaiter, originalLength, data)
        {
        }

        protected readonly AwaiterForAddedToQueue Awaiter;
        public readonly byte[] Data;
        public long OriginalLength;
    }
}