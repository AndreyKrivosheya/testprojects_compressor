using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadBlocksToWriteToBytes : Payload
    {
        public PayloadBlocksToWriteToBytes(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Func<List<BlockToWrite>, byte[]> converter)
            : base(cancellationTokenSource, settings)
        {
            this.Converter = converter;
        }

        readonly Func<List<BlockToWrite>, byte[]> Converter;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return VerifyParameterNotNullConvertAndRunUnsafe(parameter,
            (List<BlockToWrite> blocks) => new PayloadResultContinuationPending(Converter(blocks)));
        }
    }
}