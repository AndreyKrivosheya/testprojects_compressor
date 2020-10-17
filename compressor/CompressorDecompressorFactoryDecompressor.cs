using System;

namespace compressor
{
    class CompressorDecompressorFactoryDecompress: ICompressorDecompressorFactory
    {
        public Func<ProcessorQueueBlock, ProcessorQueueBlock> Create()
        {
            return ProcessorTaskDecompress.DecompressBlock;
        }
    }
}