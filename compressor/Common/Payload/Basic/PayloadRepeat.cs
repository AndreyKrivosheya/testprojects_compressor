using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadRepeat: Payload
    {
        public PayloadRepeat(CancellationTokenSource cancellationTokenSource, Payload payload)
            : base(cancellationTokenSource)
        {
            this.Payload = payload;
        }

        readonly Payload Payload;
        
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
                        Thread.Yield();
                        break;
                    case PayloadResultStatus.ContinuationPendingDoneNothing:
                        // spent the cycle checking if anything is ready to work on
                        // ... be gentle with the CPU, don't waste all onto checking if there's nothing to do
                        //WaitHandle.WaitAny(new [] { CancellationTokenSource.Token.WaitHandle }, 1000);
                        Thread.Yield();
                        break;
                    case PayloadResultStatus.Succeeded:
                    case PayloadResultStatus.Canceled:
                    case PayloadResultStatus.Failed:
                    default:
                        return payloadResult;
                }
            }

            return new PayloadResultCanceled();
        }
    }
}