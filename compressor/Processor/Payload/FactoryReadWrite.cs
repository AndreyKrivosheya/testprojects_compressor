using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    interface FactoryReadWrite
    {
        PayloadReadWrite Create(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads);
    }
}