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
    class PayloadReadWriteFromBinaryToArchive: PayloadReadWriteViaCompressDecompressDelegate
    {
        public PayloadReadWriteFromBinaryToArchive(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads)
            : base(settings, inputStream, outputStream, threads, PayloadCompress.CompressBlock)
        {
        }

        protected override void RunIdleSleep(int milliseconds)
        {
            RunIdleSleep(milliseconds, new [] { ReadingAsyncResult});
        }

        protected sealed override byte[] ProcessPendingWriteNextBlocksConvertToBytes(List<BlockToWrite> blocksToWrite)
        {
            if(blocksToWrite.Count == 1)
            {
                var blockToWrite = blocksToWrite[0];
                return ArrayExtensions.Concat(BitConverter.GetBytes(blockToWrite.Data.LongLength), blockToWrite.Data);
            }
            else
            {
                using(var blockStream = new MemoryStream((int)(blocksToWrite.Select(x => x.Data.LongLength + sizeof(long)).Sum())))
                {
                    using(var blockStreamWriter = new BinaryWriter(blockStream))
                    {
                        foreach (var block in blocksToWrite)
                        {
                            blockStreamWriter.Write(block.Data.LongLength);
                            blockStreamWriter.Write(block.Data);
                        }
                    }

                    return blockStream.ToArray();
                }
            }
        }

        BlockToProcess ReadingDataPrevious;
        BlockToProcess ReadingData;
        private IAsyncResult ReadingAsyncResult;
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
        bool? ProcessPendingReadFinishPendingRead(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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
                                ReadingData = new BlockToProcess(ReadingDataPrevious, totalRead, data.Item2.SubArray(0, totalRead));
                            }
                        }
                        else
                        {
                            ReadingData = new BlockToProcess(ReadingDataPrevious, data.Item1, data.Item2);
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
        bool? ProcessPendingReadStartNextRead(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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
        protected sealed override bool? ProcessPendingReadIfAny(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
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