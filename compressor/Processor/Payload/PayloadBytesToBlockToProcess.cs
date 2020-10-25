using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadBytesToBlockToProcess : Payload
    {
        public PayloadBytesToBlockToProcess(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings)
        {
        }

        BlockToProcess Last = null;

        protected abstract BlockToProcess ConvertBytesToBlock(BlockToProcess last, byte[] data);

        protected override PayloadResult RunUnsafe(object parameter)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            var data = parameter as byte[];
            if(data == null)
            {
                throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not byte[]", parameter), "parameter");
            }

            Last = ConvertBytesToBlock(Last, data);
            return new PayloadResultContinuationPending(Last);
        }
    }
}