using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace compressor
{
    interface IProcessorTaskFactoryReadWrite
    {
        ProcessorTaskReadWrite Create(ISettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads);
    }
}