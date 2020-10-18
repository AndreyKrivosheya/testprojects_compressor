using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor
{
    class ProcessorQueueToProcess: ProcessorQueue<ProcessorQueueBlockToProcess>
    {
        public ProcessorQueueToProcess(int maxCapacity)
            : base(maxCapacity)
        {
        }
    }
}