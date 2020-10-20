using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor
{
    class ProcessorParallelFromArchive: ProcessorParallel<Payload.FactoryReadProcessWriteFromArchive>
    {
        public ProcessorParallelFromArchive(SettingsProvider settings, Stream inputStream, Stream outputStream)
            : base(settings, inputStream, outputStream)
        {
        }
    }
}