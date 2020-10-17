namespace compressor
{
    class ProcessorTaskFactoryCompress: IProcessorTaskFactoryCompressDecompress
    {
        public ProcessorTaskCompressDescompress Create(ISettingsProvider settings)
        {
            return new ProcessorTaskCompress(settings);
        }
    }
}