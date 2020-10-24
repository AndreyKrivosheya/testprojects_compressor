using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadReadBytesExactlyStart: PayloadReadBytesStart
    {
        public PayloadReadBytesExactlyStart(CancellationTokenSource cancellationTokenSource, Stream stream, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
            : base(cancellationTokenSource, stream, exceptionProducer, onReadPastStreamEnd)
        {
        }
    }
}