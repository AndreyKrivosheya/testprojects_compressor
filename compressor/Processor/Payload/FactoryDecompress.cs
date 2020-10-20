using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    sealed class FactoryDecompress: FactoryCompressDecompress
    {
        public PayloadCompressDescompress Create(SettingsProvider settings)
        {
            return new PayloadDecompress(settings);
        }
    }
}