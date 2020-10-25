using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadReadBytesNoMoreThenStart: PayloadReadBytesStart
    {
        public PayloadReadBytesNoMoreThenStart(CancellationTokenSource cancellationTokenSource, Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
            : base(cancellationTokenSource, stream, exceptionProducer, onReadPastStreamEnd)
        {
        }
    }
}