namespace compressor
{
    sealed class ProcessorTaskFactoryDecompress: IProcessorTaskFactoryCompressDecompress
    {
        public ProcessorTaskCompressDescompress Create(ISettingsProvider settings)
        {
            return new ProcessorTaskDecompress(settings);
        }
    }
}