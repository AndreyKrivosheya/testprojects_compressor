using System;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadTrackLastIfNotNull: PayloadTrackLast
    {
        public PayloadTrackLastIfNotNull(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }

        protected override PayloadResult RunUnsafe(object parameter)
        {
            if(parameter != null)
            {
                return base.RunUnsafe(parameter);
            }
            else
            {
                return new PayloadResultContinuationPending(parameter);
            }
        }
    }
}