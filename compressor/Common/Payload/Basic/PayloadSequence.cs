using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadSequence: Payload
    {
        public PayloadSequence(CancellationTokenSource cancellationTokenSource, IEnumerable<Common.Payload.Payload> payloads)
            : base(cancellationTokenSource)
        {
            this.Payloads = new List<Payload>(payloads);
            this.PayloadsCurrent = new List<Payload>(payloads);
        }
        public PayloadSequence(CancellationTokenSource cancellationTokenSource, params Common.Payload.Payload[] payloads)
            : this(cancellationTokenSource, payloads.AsEnumerable())
        {
        }

        readonly List<Payload> Payloads;

        List<Payload> PayloadsCurrent;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            var allSucceeded = true;
            var allDoneNothing = true;
            foreach(var payload in new List<Payload>(PayloadsCurrent))
            {
                var payloadResult = payload.Run(parameter);
                switch(payloadResult.Status)
                {
                    case PayloadResultStatus.Succeeded:
                        // will not run succeedeed payload in future
                        PayloadsCurrent.Remove(payload);
                        allDoneNothing = false;
                        break;
                    case PayloadResultStatus.ContinuationPendingDoneNothing:
                        allSucceeded = false;
                        break;
                    case PayloadResultStatus.ContinuationPending:
                        allSucceeded = false;
                        allDoneNothing = false;
                        break;
                    case PayloadResultStatus.Canceled:
                    case PayloadResultStatus.Failed:
                    default:
                        return payloadResult;
                }
            }

            if(allSucceeded && allDoneNothing)
            {
                // were nothing to do actually
                return new PayloadResultSucceeded();
            }
            else
            {
                if(!allSucceeded && !allDoneNothing)
                {
                    // some work was done but not all is finished
                    return new PayloadResultContinuationPending();
                }
                else
                {
                    if(allSucceeded)
                    {
                        // all work is done
                        return new PayloadResultSucceeded();
                    }
                    else /*if(allDoneNothing)*/
                    {
                        // no work was done
                        return new PayloadResultContinuationPendingDoneNothing();
                    }
                }
            }
        }
    }
}