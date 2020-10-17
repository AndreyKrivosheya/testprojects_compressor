namespace compressor
{
    class ProcessorQueueBlock
    {
        public ProcessorQueueBlock(long offset, long originalLength, byte[] data)
        {
            this.Offset = offset;
            this.OriginalLength = originalLength;
            this.Data = data;
        }
        public ProcessorQueueBlock(ProcessorQueueBlock block, byte[] data)
            : this(block.Offset, block.OriginalLength, data)
        {
        }

        public readonly long Offset;
        public readonly long OriginalLength;
        public readonly byte[] Data;
    }
}