using System;

namespace compressor
{
    class CompressorDecompressorFactoryCompress: ICompressorDecompressorFactory
    {
        public Func<ProcessorQueueBlock, ProcessorQueueBlock> Create()
        {
            return ProcessorTaskCompress.CompressBlock;
        }
    }
}