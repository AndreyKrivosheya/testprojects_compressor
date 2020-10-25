using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadBytesToBlockToProcessBinary : PayloadBytesToBlockToProcess
    {
        public PayloadBytesToBlockToProcessBinary(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource, BytesToBlock)
        {
        }

        static BlockToProcess BytesToBlock(BlockToProcess last, byte[] data)
        {
            return new BlockToProcess(last, data.Length, data);
        }
    }
}