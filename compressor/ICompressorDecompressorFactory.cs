using System;

namespace compressor
{
    interface ICompressorDecompressorFactory
    {
        Func<ProcessorQueueBlockToProcess, ProcessorQueueBlockToWrite> Create();
    }
}