using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using compressor.Common;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadReadWriteFromArchiveToBinary: PayloadReadWriteViaCompressDecompressDelegate
    {
        public PayloadReadWriteFromArchiveToBinary(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
            : base(settings, inputStream, outputStream, threads, PayloadDecompress.DecompressBlock)
        {
        }

        protected override void RunIdleSleep(int milliseconds)
        {
            RunIdleSleep(milliseconds, new [] { ReadingLengthAsyncResult, ReadingBlockAsyncResult });
        }

        protected sealed override byte[] ProcessPendingWriteNextBlocksConvertToBytes(List<BlockToWrite> blocksToWrite)
        {
            if(blocksToWrite.Count == 1)
            {
                return blocksToWrite[0].Data;
            }
            else
            {
                using(var blockStream = new MemoryStream((int)(blocksToWrite.Select(x => x.Data.LongLength).Sum())))
                {
                    foreach (var block in blocksToWrite)
                    {
                        blockStream.Write(block.Data, 0, block.Data.Length);
                    }

                    return blockStream.ToArray();
                }
            }
        }

        BlockToProcess ReadingDataPrevious;
        BlockToProcess ReadingData;
        IAsyncResult ReadingLengthAsyncResult;
        IAsyncResult ReadingBlockAsyncResult;
        bool? ProcessPendingReadAddToQueue(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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
        bool? ProcessPendingReadFinishPendingBlockRead(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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
                            ReadingData = new BlockToProcess(ReadingDataPrevious,
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
        bool? ProcessPendingReadFinishPendingLengthRead(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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
        bool? ProcessPendingReadStartNextRead(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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
        protected sealed override bool? ProcessPendingReadIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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