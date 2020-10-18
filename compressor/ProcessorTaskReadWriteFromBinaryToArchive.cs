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

        IAsyncResult WritingAsyncResult;
        bool? ProcessPendingWriteFinishPendingWrite(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
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
        bool? ProcessPendingWriteNextBlockToWriteIfAny(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            ProcessorQueueBlockToWrite blockToWrite;
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
                using(var blockStream = new MemoryStream(sizeof(/*ProcessorQueueBlock.Data.Length*/long) + sizeof(/*ProcessorQueueBlock.Length*/long) + blockToWrite.Data.Length))
                {
                    var blockLength = (long)blockToWrite.Data.Length + sizeof(/*ProcessorQueueBlock.OriginalLength*/long);
                    var blockLengthBytes = BitConverter.GetBytes(blockLength);
                    blockStream.Write(blockLengthBytes, 0, blockLengthBytes.Length);

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
                        throw new ApplicationException("Failed to write block", e);
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
        protected sealed override bool? ProcessPendingWriteIfAny(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
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


        ProcessorQueueBlockToProcess ReadingDataPrevious;
        ProcessorQueueBlockToProcess ReadingData;
        private IAsyncResult ReadingAsyncResult;
        bool? ProcessPendingReadAddToQueue(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            try
            {
                if(queueToProcess.TryAdd(ReadingData))
                {
                    ReadingDataPrevious = ReadingData;
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
                    ReadingDataPrevious = ReadingData;
                    ReadingData = null;
                    return false;
                }
            }

            return null;
        }
        bool? ProcessPendingReadFinishPendingRead(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            if(ReadingAsyncResult.IsCompleted)
            {
                try
                {
                    var totalRead = InputStream.EndRead(ReadingAsyncResult);
                    if(totalRead != 0)
                    {
                        var data = (Tuple<long, byte[]>)ReadingAsyncResult.AsyncState;
                        if(totalRead != data.Item1)
                        {
                            if(InputStream.Position < InputStream.Length)
                            {
                                throw new ApplicationException("Failed to read block: block read in the middle is not of expected size");
                            }
                            else
                            {
                                ReadingData = new ProcessorQueueBlockToProcess(ReadingDataPrevious, totalRead, data.Item2.SubArray(0, totalRead));
                            }
                        }
                        else
                        {
                            ReadingData = new ProcessorQueueBlockToProcess(ReadingDataPrevious, data.Item1, data.Item2);
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
        bool? ProcessPendingReadStartNextRead(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            var data = Tuple.Create(Settings.BlockSize, new byte[Settings.BlockSize]);
            try
            {
                try
                {
                    ReadingAsyncResult = InputStream.BeginRead(data.Item2, 0, data.Item2.Length, null, data);
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
        protected sealed override bool? ProcessPendingReadIfAny(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
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