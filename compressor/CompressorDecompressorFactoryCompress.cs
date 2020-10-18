using System;

namespace compressor
{
    class CompressorDecompressorFactoryCompress: ICompressorDecompressorFactory
    {
        public Func<ProcessorQueueBlockToProcess, ProcessorQueueBlockToWrite> Create()
        {
            return ProcessorTaskCompress.CompressBlock;
        }
    }
}