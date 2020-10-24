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
        public PayloadBytesToBlockToProcess(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Func<BlockToProcess, byte[], BlockToProcess> converter)
            : base(cancellationTokenSource, settings)
        {
            this.Converter = converter;
        }

        readonly Func<BlockToProcess, byte[], BlockToProcess> Converter;

        BlockToProcess Last = null;

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

            Last = Converter(Last, data);
            return new PayloadResultContinuationPending(Last);
        }
    }
}