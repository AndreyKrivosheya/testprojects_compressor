using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor
{
    class ProcessorFromArchive: Processor<Payload.FactoryReadWriteFromArchiveToBinary, Payload.FactoryDecompress>
    {
        public ProcessorFromArchive(SettingsProvider settings, Stream inputStream, Stream outputStream)
            : base(settings, inputStream, outputStream)
        {
        }
    }
}