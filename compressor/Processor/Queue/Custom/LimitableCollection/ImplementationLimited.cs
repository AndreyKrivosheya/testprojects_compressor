namespace compressor.Processor.Queue.Custom.LimitableCollection
{
    class ImplementationLimited<T>: ImplementationBase<T>
    {
        public ImplementationLimited(int maxCapacity)
        {
            this.MaxCapacity = maxCapacity;
        }

        public readonly int MaxCapacity;
    }
}