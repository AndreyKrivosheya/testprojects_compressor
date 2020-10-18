using System;

namespace compressor
{
    class CompressorDecompressorFactoryDecompress: ICompressorDecompressorFactory
    {
        public Func<ProcessorQueueBlockToProcess, ProcessorQueueBlockToWrite> Create()
        {
            return ProcessorTaskDecompress.DecompressBlock;
        }
    }
}