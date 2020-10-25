using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadChain: Payload
    {
        public PayloadChain(CancellationTokenSource cancellationTokenSource, IEnumerable<Payload> payloads)
            : base(cancellationTokenSource)
        {
            this.Payloads = payloads.ToArray();
        }
        public PayloadChain(CancellationTokenSource cancellationTokenSource, params Payload[] payloads)
            : this(cancellationTokenSource, payloads.AsEnumerable())
        {
        }

        readonly IEnumerable<Payload> Payloads;
        
        IEnumerator<Payload> PayloadCurrent = null;
        object PayloadCurrentParameter = null;

        protected override PayloadResult RunUnsafe(object parameter)
        {
            if(PayloadCurrent == null)
            {
                PayloadCurrentParameter = parameter;
                PayloadCurrent = Payloads.GetEnumerator();
                if(!PayloadCurrent.MoveNext())
                {
                    return new PayloadResultSucceeded();
                }
            }

            while(PayloadCurrent != null && !CancellationTokenSource.IsCancellationRequested)
            {
                var payloadCurrentResult =  PayloadCurrent.Current.Run(PayloadCurrentParameter);
                switch(payloadCurrentResult.Status)
                {
                    case PayloadResultStatus.ContinuationPending:
                        if(PayloadCurrent.MoveNext())
                        {
                            // previous result is next argument
                            PayloadCurrentParameter = payloadCurrentResult.Result;
                            continue;
                        }
                        else
                        {
                            // reset payloads and argument
                            PayloadCurrent = null;
                            PayloadCurrentParameter = null;
                            return payloadCurrentResult;
                        }
                    case PayloadResultStatus.Succeeded:
                    case PayloadResultStatus.Canceled:
                    case PayloadResultStatus.Failed:
                    case PayloadResultStatus.ContinuationPendingDoneNothing:
                    default:
                        return payloadCurrentResult;
                }
            }

            return new PayloadResultContinuationPending();
        }
    }
}