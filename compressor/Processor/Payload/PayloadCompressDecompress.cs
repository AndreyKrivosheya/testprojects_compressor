using System;
using System.Threading;

using compressor.Common;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadCompressDescompress: Payload
    {
        protected PayloadCompressDescompress(SettingsProvider settings, Func<BlockToProcess, BlockToWrite> processor)
            : base(settings)
        {
            this.Processor = processor;
        }

        readonly Func<BlockToProcess, BlockToWrite> Processor;

        bool ProcessingCompleted;
        BlockToWrite ProcessingData;
        bool? ProcessPendingAddToQueue(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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
        bool? ProcessPendingNextBlockIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            BlockToProcess blockToProcess = null;
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
        public sealed override bool? RunOnce(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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