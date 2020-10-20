using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

using compressor.Common;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadReadWrite: Payload
    {
        protected PayloadReadWrite(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
            : base(settings)
        {
            this.InputStream = inputStream;
            this.OutputStream = outputStream;
            this.Threads = threads;
        }

        protected readonly Stream InputStream;
        protected readonly Stream OutputStream;

        protected readonly IEnumerable<Thread> Threads;

        protected override void RunIdleSleep()
        {
            RunIdleSleep(1000);
        }

        public override void Run(QueueToProcess quequeToProcess, QueueToWrite queueToWrite)
        {
            try
            {
                base.Run(quequeToProcess, queueToWrite);
            }
            finally
            {
                OutputStream.Flush();
            }
        }

        protected bool WritingCompleted = false;
        protected abstract bool? ProcessPendingWriteIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite);

        bool ProcessingCompleted = false;
        BlockToWrite ProcessingData;
        protected abstract BlockToWrite CompressDecompressBlock(BlockToProcess data);
        bool? ProcessPendingCompressDecompressAddToQueue(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(ProcessingData.WaitAllPreviousBlocksAddedToQueue(CancellationTokenSource.Token))
            {
                try
                {
                    if(queueToWrite.TryAdd(ProcessingData, CancellationTokenSource.Token))
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
                        ProcessingData = null;
                        return false;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return null;
        }
        bool? ProcessPendingCompressDecompressNextBlockIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(queueToProcess.IsCompleted)
            {
                if(!Threads.Where(x => x != Thread.CurrentThread).Any(x => x.IsAlive))
                {
                    ProcessingCompleted = true;
                    queueToWrite.CompleteAdding();
                    return false;
                }

                return null;
            }
            else
            {
                if(queueToProcess.IsHalfFull() || !Threads.Where(x => x != Thread.CurrentThread).Any(x => x.IsAlive))
                {
                    BlockToProcess blockToProcess = null;
                    bool taken = false;
                    try
                    {
                        taken = queueToProcess.TryTake(out blockToProcess);
                    }
                    catch(InvalidOperationException)
                    {
                        if(queueToProcess.IsCompleted)
                        {
                            return false;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if(taken)
                    {
                        try
                        {
                            ProcessingData = CompressDecompressBlock(blockToProcess);
                            return false;
                        }
                        catch(Exception e)
                        {
                            throw new ApplicationException("Failed to process block", e);
                        }
                    }
                }

                return null;
            }
        }
        bool? ProcessPendingCompressDecompressIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(!ProcessingCompleted || ProcessingData != null)
            {
                return new StepsRunner(
                    new StepsRunner.Step(() => ProcessingData != null, ProcessPendingCompressDecompressAddToQueue),
                    new StepsRunner.Step(ProcessPendingCompressDecompressNextBlockIfAny)
                ).Run(queueToProcess, queueToWrite);
            }
            else
            {
                return null;
            }
        }

        protected bool ReadingCompleted = false;
        protected abstract bool? ProcessPendingReadIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite);

        public sealed override bool? RunOnce(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(ReadingCompleted && ProcessingCompleted && WritingCompleted)
            {
                return true;
            }

            // if queue to process is getting bigger, need to engage
            // or if this processor is the only left, need to engage too
            var pendingProcessors = queueToProcess.IsAlmostFull() || !Threads.Where(x => x != Thread.CurrentThread).Any(x => x.IsAlive) ?
                new Func<QueueToProcess, QueueToWrite, bool?>[] {
                    ProcessPendingWriteIfAny, ProcessPendingCompressDecompressIfAny, ProcessPendingReadIfAny }: 
                new Func<QueueToProcess, QueueToWrite, bool?>[] {
                    ProcessPendingWriteIfAny, ProcessPendingReadIfAny, ProcessPendingCompressDecompressIfAny }; 
            foreach(var pendingProcessor in pendingProcessors)
            {
                var pendingProcessingResult = pendingProcessor(queueToProcess, queueToWrite);
                if(pendingProcessingResult.HasValue)
                {
                    return pendingProcessingResult.Value;
                }
            }

            return null;
        }
    };
}