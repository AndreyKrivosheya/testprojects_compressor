using System.Collections.Concurrent;

namespace compressor.Processor.Queue.Custom.LimitableCollection
{
    sealed class ImplementationUnlimited<T, TCollection>: ImplementationBase<T, TCollection>
        where TCollection: IProducerConsumerCollection<T>, new()
    {
        public ImplementationUnlimited()
        {
        }
    }
}