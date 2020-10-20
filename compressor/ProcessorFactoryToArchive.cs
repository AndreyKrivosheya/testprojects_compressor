using System.IO;

using compressor.Processor;
using compressor.Processor.Settings;

namespace compressor
{
    class ProcessorFactoryToArchive : ProcessorFactory
    {
        public Processor.Processor Create(SettingsProvider settings, Stream inputStream, Stream outputStream)
        {
            return new Processor.ProcessorParallelToArchive(settings, inputStream, outputStream);
        }
    }
}