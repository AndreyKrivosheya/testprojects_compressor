using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadWhenSucceeded: Payload
    {
        public PayloadWhenSucceeded(CancellationTokenSource cancellationTokenSource, Payload payload, Payload payloadAfterPayloadFinished)
            : base(cancellationTokenSource)
        {
            this.Payload = payload;
            this.PayloadAfterPayloadSucceeded = payloadAfterPayloadFinished;
        }

        readonly Payload Payload;
        bool PayloadSucceeded = false;
        object PayloadSucceededResult = null;
        readonly Payload PayloadAfterPayloadSucceeded;
        bool PayloadAfterPayloadSucceededSuceeded = false;
        object PayloadAfterPayloadSucceededResult = null;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            if(PayloadSucceeded && PayloadAfterPayloadSucceededSuceeded)
            {
                return new PayloadResultSucceeded(PayloadAfterPayloadSucceededResult);
            }
            else
            {
                if(!PayloadSucceeded)
                {
                    var payloadResult = Payload.Run(parameter);
                    if(payloadResult.Status == PayloadResultStatus.Succeeded)
                    {
                        PayloadSucceeded = true;
                        if(payloadResult.Result != null)
                        {
                            PayloadSucceededResult = payloadResult.Result;
                        }

                        return new PayloadResultContinuationPending(payloadResult.Result);
                    }
                    else
                    {
                        if(payloadResult.Status == PayloadResultStatus.ContinuationPending)
                        {
                            PayloadSucceededResult = payloadResult.Result;
                        }

                        return payloadResult;
                    }
                }
                else
                {
                    var payloadResult = PayloadAfterPayloadSucceeded.Run(PayloadSucceededResult);
                    if(payloadResult.Status == PayloadResultStatus.Succeeded)
                    {
                        PayloadAfterPayloadSucceededSuceeded = true;
                        PayloadAfterPayloadSucceededResult = payloadResult.Result;
                        return new PayloadResultContinuationPending(payloadResult.Result);
                    }
                    else
                    {
                        return payloadResult;
                    }
                }
            }
        }

        protected override IEnumerable<Common.Payload.Payload> GetAllImmediateSubpayloads()
        {
            if(PayloadSucceeded && PayloadAfterPayloadSucceededSuceeded)
            {
                return Enumerable.Empty<Common.Payload.Payload>();
            }
            else if(PayloadSucceeded)
            {
                return new [] { PayloadAfterPayloadSucceeded };
            }
            else
            {
                return new [] { Payload };
            }
        }
    }
}