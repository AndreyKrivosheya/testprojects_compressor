namespace compressor
{
    interface ISettingsProvider
    {
        int MaxConcurrency { get; }

        int MaxQueueSize { get; }

        long BlockSize { get; }
    }
}