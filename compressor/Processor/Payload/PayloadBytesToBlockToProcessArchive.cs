using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadBytesToBlockToProcessArchive : PayloadBytesToBlockToProcessViaDelegate
    {
        public PayloadBytesToBlockToProcessArchive(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings, BytesToBlock)
        {
        }

        static BlockToProcess BytesToBlock(BlockToProcess last, byte[] data)
        {
            return new BlockToProcess(last, BitConverter.ToInt32(data, data.Length - sizeof(Int32)), data);
        }
    }
}