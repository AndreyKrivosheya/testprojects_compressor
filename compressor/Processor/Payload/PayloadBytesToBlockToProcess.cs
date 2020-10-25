using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadBytesToBlockToProcess : Common.Payload.Payload
    {
        public PayloadBytesToBlockToProcess(CancellationTokenSource cancellationTokenSource, Func<BlockToProcess, byte[], BlockToProcess> converter)
            : base(cancellationTokenSource)
        {
            this.Converter = converter;
        }

        readonly Func<BlockToProcess, byte[], BlockToProcess> Converter;

        BlockToProcess Last = null;

        protected override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((byte[] data) =>
            {
                Last = Converter(Last, data);
                return new PayloadResultContinuationPending(Last);
            });
        }
    }
}