namespace compressor
{
    interface IProcessorTaskFactoryCompressDecompress
    {
        ProcessorTaskCompressDescompress Create(ISettingsProvider settings);
    }
}