using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace compressor
{
    class ProcessorTaskReadWriteFromArchiveToBinary: ProcessorTaskReadWrite<CompressorDecompressorFactoryDecompress>
    {
        public ProcessorTaskReadWriteFromArchiveToBinary(ISettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
            : base(settings, inputStream, outputStream, threads)
        {
        }

        protected override void RunIdleSleep(int milliseconds)
        {
            RunIdleSleep(milliseconds, new [] { WritingAsyncResult, ReadingLengthAsyncResult, ReadingBlockAsyncResult });
        }

        private IAsyncResult WritingAsyncResult;
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
                WritingAsyncResult = OutputStream.BeginWrite(blockToWrite.Data, 0, blockToWrite.Data.Length, null, null);
                return false;
            }
            else
            {
                if(queueToWrite.IsCompleted)
                {
                    WritingCompleted = true;
                    return false;
                }
            }

            return null;
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
        IAsyncResult ReadingLengthAsyncResult;
        IAsyncResult ReadingBlockAsyncResult;
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
                if(queueToProcess.IsCompleted)
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
        bool? ProcessPendingReadFinishPendingBlockRead(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            if(ReadingBlockAsyncResult.IsCompleted)
            {
                try
                {
                    var totalRead = InputStream.EndRead(ReadingBlockAsyncResult);
                    if(totalRead != 0)
                    {
                        var dataRaw = (byte[])ReadingBlockAsyncResult.AsyncState;
                        if(totalRead != dataRaw.Length)
                        {
                            throw new ApplicationException("Failed to read block: block read is of unexpected size");
                        }
                        else
                        {
                            ReadingData = new ProcessorQueueBlockToProcess(ReadingDataPrevious,
                                originalLength: BitConverter.ToInt32(dataRaw, dataRaw.Length - sizeof(Int32)),
                                data: dataRaw);
                        }
                    }
                    else
                    {
                        queueToProcess.CompleteAdding();
                        ReadingCompleted = true;
                    }
                    ReadingBlockAsyncResult = null;
                    return false;
                }
                catch(Exception e)
                {
                    throw new ApplicationException("Failed to read block", e);
                }
            }

            return null;
        }
        bool? ProcessPendingReadFinishPendingLengthRead(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            if(ReadingLengthAsyncResult.IsCompleted)
            {
                var totalRead = InputStream.EndRead(ReadingLengthAsyncResult);
                if(totalRead != 0)
                {
                    var dataLengthBuffer = (byte[])ReadingLengthAsyncResult.AsyncState;
                    if(totalRead != dataLengthBuffer.Length)
                    {
                        throw new ApplicationException("Failed to read block: failed to read block length: block read is of unexpected size");
                    }
                    else
                    {
                        var dataLength = BitConverter.ToInt64(dataLengthBuffer); 
                        var data = new byte[dataLength];
                        try
                        {
                            try
                            {
                                ReadingBlockAsyncResult = InputStream.BeginRead(data, 0, data.Length, null, data);
                            }
                            catch(IOException)
                            {
                                // reading past stream end, means we have read all the stream
                                queueToProcess.CompleteAdding();
                                ReadingCompleted = true;
                            }
                        }
                        catch(Exception e)
                        {
                            throw new ArgumentNullException("Failed to read block", e);
                        }
                    }
                }
                else
                {
                    queueToProcess.CompleteAdding();
                    ReadingCompleted = true;
                }
                ReadingLengthAsyncResult = null;
                return false;
            }

            return null;
        }
        bool? ProcessPendingReadStartNextRead(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            var buffer = new byte[sizeof(long)];
            try
            {
                try
                {
                    ReadingLengthAsyncResult = InputStream.BeginRead(buffer, 0, buffer.Length, null, buffer);
                    return false;
                }
                catch(IOException)
                {
                    // reading past stream end, means we have read all the stream
                    queueToProcess.CompleteAdding();
                    ReadingCompleted = true;
                }
            }
            catch(Exception e)
            {
                throw new ArgumentNullException("Failed to read block (length)", e);
            }

            return null;
        }
        protected sealed override bool? ProcessPendingReadIfAny(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
        {
            if(!ReadingCompleted || ReadingData != null || ReadingBlockAsyncResult != null || ReadingLengthAsyncResult != null)
            {
                return new StepsRunner(
                    new StepsRunner.Step(() => ReadingData != null, ProcessPendingReadAddToQueue),
                    new StepsRunner.Step(() => ReadingBlockAsyncResult != null, ProcessPendingReadFinishPendingBlockRead),
                    new StepsRunner.Step(() => ReadingLengthAsyncResult != null, ProcessPendingReadFinishPendingLengthRead),
                    new StepsRunner.Step(ProcessPendingReadStartNextRead)
                ).Run(queueToProcess, queueToWrite);
            }

            return null;
        }
    };
}