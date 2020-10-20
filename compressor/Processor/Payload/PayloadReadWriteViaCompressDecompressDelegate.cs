using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadReadWriteViaCompressDecompressDelegate : PayloadReadWrite
    {
        protected PayloadReadWriteViaCompressDecompressDelegate(SettingsProvider settings, Stream inputStream, Stream outputStream, IEnumerable<Thread> threads, Func<BlockToProcess, BlockToWrite> compressDecompressProcessor)
            : base(settings, inputStream, outputStream, threads)
        {
            this.CompressDecompressProcessor = compressDecompressProcessor;
        }

        readonly Func<BlockToProcess, BlockToWrite> CompressDecompressProcessor;

        protected sealed override BlockToWrite CompressDecompressBlock(BlockToProcess block)
        {
            try
            {
                return CompressDecompressProcessor(block);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to process block", e);
            }
        }
    };
}