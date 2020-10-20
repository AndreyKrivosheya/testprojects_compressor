using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class FactoryCompress: FactoryCompressDecompress
    {
        public PayloadCompressDescompress Create(SettingsProvider settings)
        {
            return new PayloadCompress(settings);
        }
    }
}