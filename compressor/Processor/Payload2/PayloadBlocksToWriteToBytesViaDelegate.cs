using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    class PayloadBlocksToWriteToBytesViaDelegate : PayloadBlocksToWriteToBytes
    {
        public PayloadBlocksToWriteToBytesViaDelegate(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Func<List<BlockToWrite>, byte[]> converter)
            : base(cancellationTokenSource, settings)
        {
            this.Converter = converter;
        }

        readonly Func<List<BlockToWrite>, byte[]> Converter;

        protected sealed override byte[] ConvertBlocksToBytes(List<BlockToWrite> blocks)
        {
            return Converter(blocks);
        }
    }
}