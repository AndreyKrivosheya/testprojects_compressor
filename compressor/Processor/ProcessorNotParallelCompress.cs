using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class ProcessorNotParallelCompress: ProcessorNotParallel
    {
        public ProcessorNotParallelCompress(SettingsProvider settings, Stream inputStream, Stream outputStream)
            : base(settings, inputStream, outputStream, PayloadFactoryCompress.Creator)
        {
        }
    }
}