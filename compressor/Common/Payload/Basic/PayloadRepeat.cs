using System.Collections.Generic;
using System.Linq;
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
                        Thread.Yield();
                        break;
                    case PayloadResultStatus.ContinuationPendingDoneNothing:
                    case PayloadResultStatus.ContinuationPendingEvaluatedToEmptyPayload:
                        // spent the cycle checking if anything is ready to work on
                        // ... be gentle with the CPU, don't waste all onto checking if there's nothing to do
                        var awaitables = GetAllWaitHandlesForRepeatAwaiting();
                        if(awaitables.Any())
                        {
                            //var awaitablesForDebug = awaitables.ToArray();
                            WaitHandle.WaitAny(Enumerable.Concat(new [] { CancellationTokenSource.Token.WaitHandle }, awaitables).ToArray(), 100);
                        }
                        else
                        {
                            Thread.Yield();
                        }
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
        
        protected override IEnumerable<Common.Payload.Payload> GetAllImmediateSubpayloads()
        {
            return new [] { Payload };
        }
    }
}