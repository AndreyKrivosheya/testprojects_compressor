using System;
using System.Threading;

namespace compressor
{
    abstract class ProcessorTaskCompressDescompress: ProcessorTask
    {
        protected ProcessorTaskCompressDescompress(ISettingsProvider settings, Func<ProcessorQueueBlockToProcess, ProcessorQueueBlockToWrite> processor)
            : base(settings)
        {
            this.Processor = processor;
        }

        readonly Func<ProcessorQueueBlockToProcess, ProcessorQueueBlockToWrite> Processor;

        bool ProcessingCompleted;
        ProcessorQueueBlockToWrite ProcessingData;
        bool? ProcessPendingAddToQueue(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            if(ProcessingData.WaitAllPreviousBlocksAddedToQueue(Timeout.Infinite, CancellationTokenSource.Token))
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
            }
            return null;
        }
        bool? ProcessPendingNextBlockIfAny(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            ProcessorQueueBlockToProcess blockToProcess = null;
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
        public sealed override bool? RunOnce(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
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