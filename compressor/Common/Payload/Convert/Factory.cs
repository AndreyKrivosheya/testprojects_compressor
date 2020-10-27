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

        public PayloadBytesToLong BytesToLong()
        {
            return new PayloadBytesToLong(CancellationTokenSource);
        }
    }
}