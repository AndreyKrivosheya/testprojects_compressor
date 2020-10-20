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
        protected override void RunIdleSleep(int milliseconds, IEnumerable<IAsyncResult> waitables)
        {
            base.RunIdleSleep(milliseconds, new [] { WritingAsyncResult }.Concat(waitables));
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
        IAsyncResult WritingAsyncResult;
        bool? ProcessPendingWriteFinishPendingWrite(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(WritingAsyncResult.IsCompleted)
            {
                try
                {
                    OutputStream.EndWrite(WritingAsyncResult);
                    WritingAsyncResult = null;
                    return false;
                }
                catch(Exception e)
                {
                    throw new ApplicationException("Failed to write block", e);
                }
            }

            return null;
        }
        protected abstract byte[] ProcessPendingWriteNextBlocksConvertToBytes(List<BlockToWrite> blocksToWrite);
        bool? ProcessPendingWriteNextBlocksToWriteIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            var blocksToWrite = new List<BlockToWrite>(Settings.MaxBlocksToWriteAtOnce);
            while(blocksToWrite.Count < blocksToWrite.Capacity)
            {
                BlockToWrite blockToWrite;
                bool taken = false;
                try
                {
                    taken = queueToWrite.TryTake(out blockToWrite);
                }
                catch(InvalidOperationException)
                {
                    if(queueToWrite.IsCompleted)
                    {
                        break;
                    }
                    else
                    {
                        throw;
                    }
                }

                if(taken)
                {
                    blocksToWrite.Add(blockToWrite);
                }
                else
                {
                    break;
                }
            }

            if(blocksToWrite.Count > 0)
            {
                try
                {
                    var block = ProcessPendingWriteNextBlocksConvertToBytes(blocksToWrite);
                    WritingAsyncResult = OutputStream.BeginWrite(block, 0, block.Length, null, null);
                    return false;
                }
                catch(Exception e)
                {
                    throw new ApplicationException("Failed to write block", e);
                }
            }
            else
            {
                if(queueToWrite.IsCompleted)
                {
                    WritingCompleted = true;
                    return false;
                }

                return null;
            }
        }
        protected bool? ProcessPendingWriteIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(!WritingCompleted || WritingAsyncResult != null)
            {
                return new StepsRunner(
                    new StepsRunner.Step(() => WritingAsyncResult != null, ProcessPendingWriteFinishPendingWrite),
                    new StepsRunner.Step(ProcessPendingWriteNextBlocksToWriteIfAny)
                ).Run(queueToProcess, queueToWrite);
            }

            return null;
        }

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