using System.IO;

using compressor.Processor;
using compressor.Processor.Settings;

namespace compressor
{
    class ProcessorFactoryFromArchive : ProcessorFactory
    {
        public Processor.Processor Create(SettingsProvider settings, Stream inputStream, Stream outputStream)
        {
            return new Processor.ProcessorFromArchive(settings, inputStream, outputStream);
        }
    }
}