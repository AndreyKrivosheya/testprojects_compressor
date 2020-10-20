using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    interface FactoryCompressDecompress
    {
        PayloadCompressDescompress Create(SettingsProvider settings);
    }
}