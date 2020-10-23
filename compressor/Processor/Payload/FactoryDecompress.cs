using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class FactoryDecompress: Factory
    {
        public PayloadCompressDescompress CreateCompressDecompress(SettingsProvider settings)
        {
            return new PayloadDecompress(settings);
        }
        public PayloadReadWrite CreateReadCompressDecompressWrite(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
        {
            return new PayloadReadWriteFromArchiveToBinary(settings, inputStream, outputStream, threads);
        }
    }
}