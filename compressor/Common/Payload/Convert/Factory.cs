using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Convert
{
    class Factory
    {
        public Factory(CancellationTokenSource cancellationTokenSource)
        {
            this.CancellationTokenSource = cancellationTokenSource;
        }

        readonly CancellationTokenSource CancellationTokenSource;

        public Common.Payload.Payload BytesToLong()
        {
            return new PayloadBytesToLong(CancellationTokenSource);
        }
    }
}