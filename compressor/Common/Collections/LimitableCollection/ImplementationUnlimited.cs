using System.Collections.Concurrent;

namespace compressor.Common.Collections.LimitableCollection
{
    sealed class ImplementationUnlimited<T, TCollection>: ImplementationBase<T, TCollection>
        where TCollection: IProducerConsumerCollection<T>, new()
    {
        public ImplementationUnlimited()
        {
        }

        public sealed override int MaxCapacity
        {
            get
            {
                return -1;
            }
        }
    }
}