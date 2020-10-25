using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    abstract class Payload: Common.Payload.Payload
    {
        public Payload(CancellationTokenSource cancellationTokenSource, Stream stream)
            : base(cancellationTokenSource)
        {
            this.Stream = stream;
        }

        protected readonly Stream Stream;
   }
}