using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadBytesToBlockToProcessViaDelegate : PayloadBytesToBlockToProcess
    {
        public PayloadBytesToBlockToProcessViaDelegate(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Func<BlockToProcess, byte[], BlockToProcess> converter)
            : base(cancellationTokenSource, settings)
        {
            this.Converter = converter;
        }

        readonly Func<BlockToProcess, byte[], BlockToProcess> Converter;

        protected sealed override BlockToProcess ConvertBytesToBlock(BlockToProcess last, byte[] data)
        {
            return Converter(last, data);
        }
    }
}