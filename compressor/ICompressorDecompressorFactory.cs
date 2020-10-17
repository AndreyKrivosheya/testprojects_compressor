using System;

namespace compressor
{
    interface ICompressorDecompressorFactory
    {
        Func<ProcessorQueueBlock, ProcessorQueueBlock> Create();
    }
}