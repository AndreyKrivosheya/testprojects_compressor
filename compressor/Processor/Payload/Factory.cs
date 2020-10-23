using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    interface Factory
    {
        PayloadCompressDescompress CreateCompressDecompress(SettingsProvider settings);
        PayloadReadWrite CreateReadCompressDecompressWrite(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads);
    }
}