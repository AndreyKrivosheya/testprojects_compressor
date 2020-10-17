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

        public override void Run(ProcessorQueue quequeToProcess, ProcessorQueue queueToWrite)
        {
            // prepare output
            // ... set length
            // ... ... read length from input
            var originalStreamLength = new byte[sizeof(long)];
            var originalStreamLengthRead = InputStream.Read(originalStreamLength, 0, originalStreamLength.Length);
            if(originalStreamLengthRead != originalStreamLength.Length)
            {
                throw new IOException("Failed to read original size");
            }
            var originalLength = BitConverter.ToInt64(originalStreamLength);
            // ... ... set output stream size and rewind
            OutputStream.SetLength(originalLength);
            OutputStream.Seek(0, SeekOrigin.Begin);
            // perform read/process/write
            base.Run(quequeToProcess, queueToWrite);
        }

        private IAsyncResult WritingAsyncResult;
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
                OutputStream.Seek(blockToWrite.Offset, SeekOrigin.Begin);
                WritingAsyncResult = OutputStream.BeginWrite(blockToWrite.Data, 0, (int)Math.Min(blockToWrite.Data.Length, blockToWrite.OriginalLength), null, null);
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
        private IAsyncResult ReadingLengthAsyncResult;
        private IAsyncResult ReadingBlockAsyncResult;
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
                if(queueToProcess.IsCompleted)
                {
                    // something wrong: queue-to-process is closed for additions, but there's block outstanding
                    // probably there's an exception on another worker thread
                    ReadingData = null;
                    return false;
                }
            }

            return null;
        }
        bool? ProcessPendingReadFinishPendingBlockRead(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
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
                            var off = 0;
                            var offset = BitConverter.ToInt64(dataRaw, off);
                            off += sizeof(long);
                            var originalLength = BitConverter.ToInt64(dataRaw, off);
                            off += sizeof(long);
                            var data = dataRaw.SubArray(off);

                            ReadingData = new ProcessorQueueBlock(offset, originalLength, data);
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
        bool? ProcessPendingReadFinishPendingLengthRead(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
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
        bool? ProcessPendingReadStartNextRead(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
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
        protected sealed override bool? ProcessPendingReadIfAny(ProcessorQueue queueToProcess, ProcessorQueue queueToWrite)
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