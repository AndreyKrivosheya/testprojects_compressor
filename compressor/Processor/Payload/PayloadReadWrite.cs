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
        RunOnceResult ProcessPendingWriteFinishPendingWrite(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(WritingAsyncResult.IsCompleted)
            {
                try
                {
                    OutputStream.EndWrite(WritingAsyncResult);
                    WritingAsyncResult = null;
                    return RunOnceResult.WorkDoneButNotFinished;
                }
                catch(Exception e)
                {
                    throw new ApplicationException("Failed to write block", e);
                }
            }

            return RunOnceResult.DoneNothing;
        }
        protected abstract byte[] ProcessPendingWriteNextBlocksConvertToBytes(List<BlockToWrite> blocksToWrite);
        RunOnceResult ProcessPendingWriteNextBlocksToWriteIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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
                    return RunOnceResult.WorkDoneButNotFinished;
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
                    return RunOnceResult.WorkDoneButNotFinished;
                }

                return RunOnceResult.DoneNothing;
            }
        }
        protected RunOnceResult ProcessPendingWriteIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(!WritingCompleted || WritingAsyncResult != null)
            {
                if(WritingAsyncResult != null)
                {
                    return ProcessPendingWriteFinishPendingWrite(queueToProcess, queueToWrite);
                }
                else
                {
                    return ProcessPendingWriteNextBlocksToWriteIfAny(queueToProcess, queueToWrite);
                };
            }

            return RunOnceResult.DoneNothing;
        }

        bool ProcessingCompleted = false;
        BlockToWrite ProcessingData;
        protected abstract BlockToWrite CompressDecompressBlock(BlockToProcess data);
        RunOnceResult ProcessPendingCompressDecompressAddToQueue(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(ProcessingData.WaitAllPreviousBlocksAddedToQueue(CancellationTokenSource.Token))
            {
                try
                {
                    if(queueToWrite.TryAdd(ProcessingData, CancellationTokenSource.Token))
                    {
                        ProcessingData = null;
                        return RunOnceResult.WorkDoneButNotFinished;
                    }
                }
                catch(InvalidOperationException)
                {
                    if(queueToWrite.IsAddingCompleted)
                    {
                        // something wrong: queue-to-write is closed for additions, but there's block outstanding
                        // probably there's an exception on another worker thread
                        ProcessingData = null;
                        return RunOnceResult.WorkDoneButNotFinished;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return RunOnceResult.DoneNothing;
        }
        RunOnceResult ProcessPendingCompressDecompressNextBlockIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(queueToProcess.IsCompleted)
            {
                if(!Threads.Where(x => x != Thread.CurrentThread).Any(x => x.IsAlive))
                {
                    ProcessingCompleted = true;
                    queueToWrite.CompleteAdding();
                    return RunOnceResult.WorkDoneButNotFinished;
                }

                return RunOnceResult.DoneNothing;
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
                            return RunOnceResult.WorkDoneButNotFinished;
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
                            return RunOnceResult.WorkDoneButNotFinished;
                        }
                        catch(Exception e)
                        {
                            throw new ApplicationException("Failed to process block", e);
                        }
                    }
                }

                return RunOnceResult.DoneNothing;
            }
        }
        RunOnceResult ProcessPendingCompressDecompressIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(!ProcessingCompleted || ProcessingData != null)
            {
                if(ProcessingData != null)
                {
                    return ProcessPendingCompressDecompressAddToQueue(queueToProcess, queueToWrite);
                }
                else
                {
                    return ProcessPendingCompressDecompressNextBlockIfAny(queueToProcess, queueToWrite);
                }
            }
            else
            {
                return RunOnceResult.DoneNothing;
            }
        }

        protected bool ReadingCompleted = false;
        protected abstract RunOnceResult ProcessPendingReadIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite);

        protected sealed override RunOnceResult RunOnce(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            if(ReadingCompleted && ProcessingCompleted && WritingCompleted)
            {
                return RunOnceResult.Finished;
            }

            // if queue to process is getting bigger, need to engage
            // or if this processor is the only left, need to engage too
            var pendingProcessors = queueToProcess.IsAlmostFull() || !Threads.Where(x => x != Thread.CurrentThread).Any(x => x.IsAlive) ?
                new Func<QueueToProcess, QueueToWrite, RunOnceResult>[] {
                    ProcessPendingWriteIfAny, ProcessPendingCompressDecompressIfAny, ProcessPendingReadIfAny }: 
                new Func<QueueToProcess, QueueToWrite, RunOnceResult>[] {
                    ProcessPendingWriteIfAny, ProcessPendingReadIfAny, ProcessPendingCompressDecompressIfAny }; 
            foreach(var pendingProcessor in pendingProcessors)
            {
                var pendingProcessingResult = pendingProcessor(queueToProcess, queueToWrite);
                if(pendingProcessingResult != RunOnceResult.DoneNothing)
                {
                    return pendingProcessingResult;
                }
            }

            return RunOnceResult.DoneNothing;
        }
    };
}