using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Convert
{
    abstract class Payload: Common.Payload.Payload
    {
        public Payload(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }
   }
}