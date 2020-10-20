using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    sealed class FactoryReadWriteFromBinaryToArchive: FactoryReadWrite
    {
        public PayloadReadWrite Create(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
        {
            return new PayloadReadWriteFromBinaryToArchive(settings, inputStream, outputStream, threads);
        }
    }
}