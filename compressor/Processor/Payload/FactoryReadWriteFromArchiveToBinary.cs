using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    sealed class FactoryReadWriteFromArchiveToBinary: FactoryReadWrite
    {
        public PayloadReadWrite Create(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
        {
            return new PayloadReadWriteFromArchiveToBinary(settings, inputStream, outputStream, threads);
        }
    }
}