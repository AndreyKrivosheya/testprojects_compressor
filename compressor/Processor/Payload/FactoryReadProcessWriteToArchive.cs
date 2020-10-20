using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class FactoryReadProcessWriteToArchive: FactoryReadProcessWrite<FactoryReadWriteFromBinaryToArchive, FactoryCompress>
    {
    }
}