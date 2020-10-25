using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadFlush: Payload
    {
        public PayloadFlush(CancellationTokenSource cancellationTokenSource, Stream stream)
            : base(cancellationTokenSource, stream)
        {
        }

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            Stream.Flush();
            return new PayloadResultContinuationPending();
        }
    }
}