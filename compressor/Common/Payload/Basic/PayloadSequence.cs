using System;
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
            this.Payloads = new List<PayloadWithFinishedState>(payloads.Select(x => new PayloadWithFinishedState(x)));
        }
        public PayloadSequence(CancellationTokenSource cancellationTokenSource, params Common.Payload.Payload[] payloads)
            : this(cancellationTokenSource, payloads.AsEnumerable())
        {
        }

        class PayloadWithFinishedState
        {
            public PayloadWithFinishedState(Common.Payload.Payload payload)
            {
                this.Payload = payload;
            }

            public readonly Common.Payload.Payload Payload;
            public bool Finished = false;
        }

        readonly List<PayloadWithFinishedState> Payloads;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            var allSucceeded = true;
            var allDoneNothing = true;
            foreach(var payload in Payloads)
            {
                if(!payload.Finished)
                {
                    var payloadResult = payload.Payload.Run(parameter);
                    switch(payloadResult.Status)
                    {
                        case PayloadResultStatus.Succeeded:
                            // will not run succeedeed payload in future
                            payload.Finished = true;
                            // ...
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