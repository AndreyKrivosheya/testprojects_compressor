using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace compressor
{
    sealed class ProcessorTaskFactoryReadWriteFromBinaryToArchive: IProcessorTaskFactoryReadWrite
    {
        public ProcessorTaskReadWrite Create(ISettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
        {
            return new ProcessorTaskReadWriteFromBinaryToArchive(settings, inputStream, outputStream, threads);
        }
    }
}