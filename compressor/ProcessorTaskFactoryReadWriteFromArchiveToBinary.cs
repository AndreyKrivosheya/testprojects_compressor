using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace compressor
{
    sealed class ProcessorTaskFactoryReadWriteFromArchiveToBinary: IProcessorTaskFactoryReadWrite
    {
        public ProcessorTaskReadWrite Create(ISettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
        {
            return new ProcessorTaskReadWriteFromArchiveToBinary(settings, inputStream, outputStream, threads);
        }
    }
}