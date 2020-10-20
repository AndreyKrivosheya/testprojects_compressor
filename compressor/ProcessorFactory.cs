using System.IO;

using compressor.Processor;
using compressor.Processor.Settings;

namespace compressor
{
    interface ProcessorFactory
    {
        Processor.Processor Create(SettingsProvider settings, Stream inputStream, Stream outputStream);
    }
}