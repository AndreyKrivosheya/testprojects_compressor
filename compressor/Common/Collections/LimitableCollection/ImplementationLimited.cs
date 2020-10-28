using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Common.Collections.LimitableCollection
{
    sealed class ImplementationLimited<T, TCollection>: ImplementationBase<T, TCollection>
        where TCollection: IProducerConsumerCollection<T>, new()
    {
        public ImplementationLimited(int maxCapacity)
        {
            this.MaxCapacityBackingField = maxCapacity;
            this.ProducersSemaphore = new Semaphore(maxCapacity, maxCapacity);
            this.ProducersCancellationTokenSource = new CancellationTokenSource();
        }

        readonly CancellationTokenSource ProducersCancellationTokenSource;
        readonly Semaphore ProducersSemaphore;

        protected sealed override void DisposeCollection()
        {
            ProducersCancellationTokenSource.Dispose();
            ProducersSemaphore.Close();

            base.DisposeCollection();
        }

        readonly int MaxCapacityBackingField;
        public override int MaxCapacity
        {
            get
            {
                return MaxCapacityBackingField;
            }
        }

        protected sealed override bool TryAddToCollection(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // try getting semaphore with no wait if possible or go for full waiting
            var waitSucceeded = ProducersSemaphore.WaitOne(0);
            if(!waitSucceeded && millisecondsTimeout != 0)
            {
                var waitResult = WaitHandle.WaitAny(new [] { ProducersSemaphore, cancellationToken.WaitHandle, ProducersCancellationTokenSource.Token.WaitHandle }, millisecondsTimeout);
                if(waitResult == WaitHandle.WaitTimeout)
                {
                    return false;
                }
                else
                {
                    switch(waitResult)
                    {
                        case 0:
                            waitSucceeded = true;
                            break;
                        case 1:
                            // canceled through external cancellation token
                            throw new OperationCanceledException();
                        case 2:
                        default:
                            // canceled through internal cancellation token (due to concurrent with CompleteAdding)
                            throw new InvalidOperationException("Can't add to collection for which adding was completed");
                    }
                }
            }

            if(waitSucceeded)
            {
                try
                {
                    if(!base.TryAddToCollection(item, millisecondsTimeout, cancellationToken))
                    {
                        ProducersSemaphore.Release();
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch(Exception)
                {
                    ProducersSemaphore.Release();
                    throw;
                }
            }
            else
            {
                return false;
            }
        }

        protected sealed override bool CompleteAddingToCollection()
        {
            if(base.CompleteAddingToCollection())
            {
                // wake up adders
                ProducersCancellationTokenSource.Cancel();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected sealed override bool TryTakeFromCollection(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if(base.TryTakeFromCollection(out item, millisecondsTimeout, cancellationToken))
            {
                ProducersSemaphore.Release();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}