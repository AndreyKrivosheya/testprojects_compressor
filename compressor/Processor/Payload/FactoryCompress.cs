using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class FactoryCompress: Factory
    {
        public PayloadCompressDescompress CreateCompressDecompress(SettingsProvider settings)
        {
            return new PayloadCompress(settings);
        }
        public PayloadReadWrite CreateReadCompressDecompressWrite(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
        {
            return new PayloadReadWriteFromBinaryToArchive(settings, inputStream, outputStream, threads);
        }
    }
}