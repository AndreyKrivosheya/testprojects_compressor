namespace compressor.Common.Collections
{
    class AsyncLimitableQueue<T>: AsyncLimitableCollection<T, LimitableQueue<T>>
    {
        public AsyncLimitableQueue(int maxCapacity)
            : base(maxCapacity)
        {
        }
    };
}