using System;
using System.Threading;

namespace compressor
{
    class ProcessorQueueBlockToProcess: ProcessorQueueBlock
    {
        public ProcessorQueueBlockToProcess(ProcessorQueueBlockToProcess previousBlock, long originalLength, byte[] data)
            : base(previousBlock != null ? new AwaiterForAddedToQueue(previousBlock.Awaiter) : new AwaiterForAddedToQueue(), originalLength, data)
        {
        }
    }
}