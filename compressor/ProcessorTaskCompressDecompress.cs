using System;
using System.Threading;

namespace compressor
{
    abstract class ProcessorTaskCompressDescompress: ProcessorTask
    {
        protected ProcessorTaskCompressDescompress(ISettingsProvider settings, Func<ProcessorQueueBlock, ProcessorQueueBlock> processor)
            : base(settings)
        {
            this.Processor = processor;
        }

        readonly Func<ProcessorQueueBlock, ProcessorQueueBlock> Processor;

        bool ProcessingCompleted;
        ProcessorQueueBlock ProcessingData;
        bool? ProcessPendingAddToQueue(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            try
            {
                if(queueToWrite.TryAdd(ProcessingData, Timeout.Infinite, CancellationTokenSource.Token))
                {
                    ProcessingData = null;
                    return false;
                }
            }
            catch(InvalidOperationException)
            {
                if(queueToWrite.IsAddingCompleted)
                {
                    // something wrong: queue-to-write is closed for additions, but there's block outstanding
                    // probably there's an exception on another worker thread
                    ProcessingCompleted = true;
                    ProcessingData = null;
                }
                else
                {
                    throw;
                }
            }

            return null;
        }
        bool? ProcessPendingNextBlockIfAny(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            ProcessorQueueBlock blockToProcess = null;
            bool taken = false;
            try
            {
                taken = queueToProcess.TryTake(out blockToProcess, Timeout.Infinite, CancellationTokenSource.Token);
            }
            catch(InvalidOperationException)
            {
                if(queueToProcess.IsCompleted)
                {
                    ProcessingCompleted = true;
                    return false;
                }
                else
                {
                    throw;
                }
            }

            if(taken)
            {
                ProcessingData = Processor(blockToProcess);
                return false;
            }
            else
            {
                if(queueToProcess.IsCompleted)
                {
                    ProcessingCompleted = true;
                    return false;
                }
            }

            return null;
        } 
        public sealed override bool? RunOnce(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            if(!ProcessingCompleted || ProcessingData != null)
            {
                return new StepsRunner(
                    new StepsRunner.Step(() => ProcessingData != null, ProcessPendingAddToQueue),
                    new StepsRunner.Step(ProcessPendingNextBlockIfAny)
                ).Run(queueToProcess, queueToWrite);
            }
            else
            {
                return true;
            }
        }
    }
}