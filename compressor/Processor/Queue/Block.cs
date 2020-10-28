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
                if(this.Previous != null)
                {
                    //this.A = this.Previous.A + 1;
                    this.Previous.Last = false;
                }
                else
                {
                    //this.A = 0;
                }
            }
            public AwaiterForAddedToQueue()
                : this(null)
            {
            }

            //readonly int A;

            AwaiterForAddedToQueue Previous = null;

            public bool Last { get; private set; } = true;

            readonly object BlockAddedToQueueToWriteLazyLock = new object();
            Lazy<ManualResetEvent> BlockAddedToQueueToWriteLazy = new Lazy<ManualResetEvent>(() => new ManualResetEvent(false));
            
            public void NotifyProcessedAndAddedToQueueToWrite()
            {
                lock(BlockAddedToQueueToWriteLazyLock)
                {
                    if(BlockAddedToQueueToWriteLazy.IsValueCreated)
                    {
                        if(BlockAddedToQueueToWriteLazy.Value != null)
                        {
                            BlockAddedToQueueToWriteLazy.Value.Set();
                        }
                    }
                    Previous = null;
                    BlockAddedToQueueToWriteLazy = new Lazy<ManualResetEvent>(() => null);
                }
            }

            public bool WaitAllPreviousBlocksProcessedAddedToQueueToWrite(int millisecondsTimeout, CancellationToken cancellationToken)
            {
                var previous = Previous;
                if(null != previous)
                {
                    if(!previous.WaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(millisecondsTimeout, cancellationToken))
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
            public bool WaitAllPreviousBlocksProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken)
            {
                return WaitAllPreviousBlocksProcessedAddedToQueueToWrite(0, cancellationToken);
            }

            public bool WaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(int millisecondsTimeout, CancellationToken cancellationToken)
            {
                // wait all previous blocks were added to queue
                if(!WaitAllPreviousBlocksProcessedAddedToQueueToWrite(millisecondsTimeout, cancellationToken))
                {
                    return false;
                }
                // wait this block to be added to queue
                WaitHandle waitableThisBlockAddedToQueue;
                lock(BlockAddedToQueueToWriteLazyLock)
                {
                    waitableThisBlockAddedToQueue = BlockAddedToQueueToWriteLazy.Value;
                }
                if(waitableThisBlockAddedToQueue != null)
                {
                    bool waitingEndedDueToAddedToQueue = false;
                    bool waitingEndedDueToWaitableDisposed = false;
                    try
                    {
                        if(0 == WaitHandle.WaitAny(new [] { waitableThisBlockAddedToQueue, cancellationToken.WaitHandle }, millisecondsTimeout))
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
                            lock(BlockAddedToQueueToWriteLazyLock)
                            {
                                BlockAddedToQueueToWriteLazy = new Lazy<ManualResetEvent>(() => null);
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
            public bool WaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken)
            {
                return WaitThisAndAllPreviousBlocksProcessedAndAddedToQueueToWrite(0, cancellationToken);
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