using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Convert
{
    abstract class PayloadBytes: Common.Payload.Payload
    {
        public PayloadBytes(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }

        protected abstract PayloadResult RunUnsafe(byte[] data);
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return VerifyParameterNotNullConvertAndRunUnsafe(parameter,
            ((Func<byte[], PayloadResult>)RunUnsafe));
        }
   }
}