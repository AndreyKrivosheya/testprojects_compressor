using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadRepeat: Payload
    {
        public PayloadRepeat(CancellationTokenSource cancellationTokenSource, Payload payload)
            : base(cancellationTokenSource)
        {
            this.Payload = payload;
            this.AwaiterWhenNothingDone = new SpinWait();
            this.AwaiterWhenNothingDone.Reset();
        }

        readonly Payload Payload;
        readonly SpinWait AwaiterWhenNothingDone;
        protected override PayloadResult RunUnsafe(object parameter)
        {
            while(!CancellationTokenSource.IsCancellationRequested)
            {
                var payloadResult = Payload.Run(parameter);
                switch(payloadResult.Status)
                {
                    case PayloadResultStatus.ContinuationPending:
                        // some work was done, but that doesn't completed payload
                        // ... to the next runcycle
                        AwaiterWhenNothingDone.Reset();
                        AwaiterWhenNothingDone.SpinOnce();
                        break;
                    case PayloadResultStatus.ContinuationPendingDoneNothing:
                    case PayloadResultStatus.ContinuationPendingEvaluatedToEmptyPayload:
                        // spent the cycle checking if anything is ready to work on
                        // ... to the next runcycle
                        AwaiterWhenNothingDone.SpinOnce();
                        break;
                    case PayloadResultStatus.Succeeded:
                    case PayloadResultStatus.Canceled:
                    case PayloadResultStatus.Failed:
                    default:
                        AwaiterWhenNothingDone.Reset();
                        return payloadResult;
                }
            }

            return new PayloadResultCanceled();
        }
    }
}