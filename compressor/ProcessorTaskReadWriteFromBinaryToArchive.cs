using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace compressor
{
    class ProcessorTaskReadWriteFromBinaryToArchive: ProcessorTaskReadWrite<CompressorDecompressorFactoryCompress>
    {
        public ProcessorTaskReadWriteFromBinaryToArchive(ISettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
            : base(settings, inputStream, outputStream, threads)
        {
        }

        protected override void RunIdleSleep(int milliseconds)
        {
            RunIdleSleep(milliseconds, new [] { WritingAsyncResult, ReadingAsyncResult});
        }

        public override void Run(ProcessorQueue quequeToProcess, ProcessorQueue queueToWrite)
        {
            // prepare output file
            // ... reset length
            OutputStream.SetLength(0);
            // ... write length
            var inputStreamLength = BitConverter.GetBytes(InputStream.Length);
            OutputStream.Write(inputStreamLength, 0, inputStreamLength.Length);
            // perform read/process/write
            try
            {
                base.Run(quequeToProcess, queueToWrite);
            }
            finally
            {
                OutputStream.Flush();
            }
        }

        IAsyncResult WritingAsyncResult;
        bool? ProcessPendingWriteFinishPendingWrite(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
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
        bool? ProcessPendingWriteNextBlockToWriteIfAny(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            ProcessorQueueBlock blockToWrite;
            bool taken = false;
            try
            {
                taken = queueToWrite.TryTake(out blockToWrite);
            }
            catch(InvalidOperationException)
            {
                if(queueToWrite.IsCompleted)
                {
                    WritingCompleted = true;
                    return false;
                }
                else
                {
                    throw;
                }
            }

            if(taken)
            {
                using(var blockStream = new MemoryStream(sizeof(/*ProcessorQueueBlock.Data.Length*/long) + sizeof(/*ProcessorQueueBlock.Offset*/long) + sizeof(/*ProcessorQueueBlock.OriginalLength*/long) + blockToWrite.Data.Length))
                {
                    var blockLength = (long)blockToWrite.Data.Length + sizeof(/*ProcessorQueueBlock.Offset*/long) + sizeof(/*ProcessorQueueBlock.OriginalLength*/long);
                    var blockLengthBytes = BitConverter.GetBytes(blockLength);
                    blockStream.Write(blockLengthBytes, 0, blockLengthBytes.Length);

                    var blockOffsetBytes = BitConverter.GetBytes(blockToWrite.Offset);
                    blockStream.Write(blockOffsetBytes, 0, blockOffsetBytes.Length);
                    var blockOriginalLengthBytes = BitConverter.GetBytes(blockToWrite.OriginalLength);
                    blockStream.Write(blockOriginalLengthBytes, 0, blockOriginalLengthBytes.Length);
                    var blockData = blockToWrite.Data;
                    blockStream.Write(blockData, 0, blockData.Length);

                    var block = blockStream.ToArray();
                    try
                    {
                        WritingAsyncResult = OutputStream.BeginWrite(block, 0, block.Length, null, null);
                        return false;
                    }
                    catch(Exception e)
                    {
                        throw new ApplicationException("Faild to write block", e);
                    }
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
        protected sealed override bool? ProcessPendingWriteIfAny(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            if(!WritingCompleted || WritingAsyncResult != null)
            {
                return new StepsRunner(
                    new StepsRunner.Step(() => WritingAsyncResult != null, ProcessPendingWriteFinishPendingWrite),
                    new StepsRunner.Step(ProcessPendingWriteNextBlockToWriteIfAny)
                ).Run(queueToProcess, queueToWrite);
            }

            return null;
        }

        private ProcessorQueueBlock ReadingData;
        private IAsyncResult ReadingAsyncResult;
        bool? ProcessPendingReadAddToQueue(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            try
            {
                if(queueToProcess.TryAdd(ReadingData))
                {
                    ReadingData = null;
                    return false;
                }
            }
            catch(InvalidOperationException)
            {
                if(queueToProcess.IsAddingCompleted)
                {
                    // something wrong: queue-to-process is closed for additions, but there's block outstanding
                    // probably there's an exception on another worker thread
                    ReadingData = null;
                    return false;
                }
            }

            return null;
        }
        bool? ProcessPendingReadFinishPendingRead(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            if(ReadingAsyncResult.IsCompleted)
            {
                try
                {
                    var totalRead = InputStream.EndRead(ReadingAsyncResult);
                    if(totalRead != 0)
                    {
                        ReadingData = (ProcessorQueueBlock)ReadingAsyncResult.AsyncState;
                        if(totalRead != ReadingData.OriginalLength)
                        {
                            if(InputStream.Position < InputStream.Length)
                            {
                                throw new ApplicationException("Failed to read block: block read in the middle is not of expected size");
                            }
                            else
                            {
                                ReadingData = new ProcessorQueueBlock(ReadingData.Offset, totalRead, ReadingData.Data.SubArray(0, totalRead));
                            }
                        }
                    }
                    else
                    {
                        queueToProcess.CompleteAdding();
                        ReadingCompleted = true;
                    }
                    ReadingAsyncResult = null;
                    return false;
                }
                catch(Exception e)
                {
                    throw new ApplicationException("Failed to read block", e);
                }
            }

            return null;
        }
        bool? ProcessPendingReadStartNextRead(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            var data = new ProcessorQueueBlock(InputStream.Position, Settings.BlockSize, new byte[Settings.BlockSize]);
            try
            {
                try
                {
                    ReadingAsyncResult = InputStream.BeginRead(data.Data, 0, data.Data.Length, null, data);
                    return false;
                }
                catch(IOException)
                {
                    // reading past stream end, means we have read all the stream
                    queueToProcess.CompleteAdding();
                    ReadingCompleted = true;
                    return false;
                }
            }
            catch(Exception e)
            {
                throw new ArgumentNullException("Failed to read block", e);
            }
        }
        protected sealed override bool? ProcessPendingReadIfAny(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
        {
            if(!ReadingCompleted || ReadingData != null || ReadingAsyncResult != null)
            {
                return new StepsRunner(
                    new StepsRunner.Step(() => ReadingData != null, ProcessPendingReadAddToQueue),
                    new StepsRunner.Step(() => ReadingAsyncResult != null, ProcessPendingReadFinishPendingRead),
                    new StepsRunner.Step(ProcessPendingReadStartNextRead)
                ).Run(queueToProcess, queueToWrite);
            }

            return null;
        }
    };
}