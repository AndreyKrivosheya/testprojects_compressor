using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    interface FactoryReadProcessWrite
    {
        PayloadCompressDescompress CreateCompressDecompress(SettingsProvider settings);
        PayloadReadWrite CreateReadCompressDecompressWrite(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads);
    }

    class FactoryReadProcessWrite<TFactoryReadWrite, TFactoryCompressDecompress> : FactoryReadProcessWrite
        where TFactoryReadWrite: FactoryReadWrite, new()
        where TFactoryCompressDecompress: FactoryCompressDecompress, new()
    {
        readonly FactoryCompressDecompress FactoryCompressDecompressInstance = new TFactoryCompressDecompress();
        public PayloadCompressDescompress CreateCompressDecompress(SettingsProvider settings)
        {
            return FactoryCompressDecompressInstance.Create(settings);
        }

        readonly FactoryReadWrite FactoryReadProcessWriteInstance = new TFactoryReadWrite();
        public PayloadReadWrite CreateReadCompressDecompressWrite(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
        {
            return FactoryReadProcessWriteInstance.Create(settings, inputStream, outputStream, threads);
        }
    }
}