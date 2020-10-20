using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class FactoryReadProcessWriteFromArchive: FactoryReadProcessWrite<FactoryReadWriteFromArchiveToBinary, FactoryDecompress>
    {
    }
}