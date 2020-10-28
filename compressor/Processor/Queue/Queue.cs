using System;
using System.Threading;

using compressor.Common.Collections;

namespace compressor.Processor.Queue
{
    class Queue<TBlock> : IDisposable
        where TBlock: Block
    {
        public Queue(int maxCapacity)
        {
            if(maxCapacity < 1)
            {
                throw new ArgumentException("Can't limit collection to less than 1 item", "maxCapacity");
            }

            this.Implementation = new AsyncLimitableQueue<TBlock>(maxCapacity);
        }

        readonly AsyncLimitableCollection<TBlock> Implementation;

        public void Dispose()
        {
            Implementation.Dispose();
        }

        public virtual IAsyncResult BeginAdd(TBlock block, CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            return Implementation.BeginAdd(block, cancellationToken, asyncCallback, state);
        }
        public virtual IAsyncResult BeginAdd(TBlock block, AsyncCallback asyncCallback = null, object state = null)
        {
            return Implementation.BeginAdd(block, asyncCallback, state);
        }

        public void EndAdd(IAsyncResult addingAsyncResult)
        {
            Implementation.EndAdd(addingAsyncResult);
        }

        public bool IsAddingCompleted
        {
            get
            {
                return Implementation.IsCompleted;
            }
        }
        
        public void CompleteAdding()
        {
            Implementation.CompleteAdding();
        }

        public IAsyncResult BeginTake(CancellationToken cancellationToken, AsyncCallback asyncCallback = null, object state = null)
        {
            return Implementation.BeginTake(cancellationToken, asyncCallback, state);
        }
        public IAsyncResult BeginTake(AsyncCallback asyncCallback = null, object state = null)
        {
            return Implementation.BeginTake(asyncCallback, state);
        }

        public TBlock EndTake(IAsyncResult takingAsyncResult)
        {
            return Implementation.EndTake(takingAsyncResult);
        }

        public int Count
        {
            get
            {
                return Implementation.Count;
            }
        }

        public int MaxCapacity
        {
            get
            {
                return Implementation.MaxCapacity;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Implementation.IsCompleted;
            }
        }
        
        public bool IsPercentsFull(int percents)
        {
            if(percents < 0 || percents > 100)
            {
                throw new ArgumentException("percents");
            }
            
            if(MaxCapacity < 1)
            {
                return false;
            }
            else
            {
                return Count >= ((percents * MaxCapacity) / 100f);
            }
        }

        public bool IsHalfFull()
        {
            return IsPercentsFull(50);
        }
        public bool IsAlmostFull()
        {
            return IsPercentsFull(90);
        }
        public bool IsFull()
        {
            return IsPercentsFull(99);
        }
    }
}