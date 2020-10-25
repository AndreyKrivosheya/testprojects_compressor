using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Convert
{
    class PayloadBytesToLong: PayloadBytes
    {
        public PayloadBytesToLong(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }

        protected override PayloadResult RunUnsafe(byte[] data)
        {
            return new PayloadResultContinuationPending(BitConverter.ToInt64(data));
        }
   }
}