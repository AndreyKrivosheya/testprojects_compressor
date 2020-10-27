using System;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadTrackLast: Payload
    {
        public PayloadTrackLast(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }

        public object Last { get; private set; } = null;

        protected override PayloadResult RunUnsafe(object parameter)
        {
            Last = parameter;
            return new PayloadResultContinuationPending(parameter);
        }
    }
}