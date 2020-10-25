using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadWhenFinished: Payload
    {
        public PayloadWhenFinished(CancellationTokenSource cancellationTokenSource, Payload payload, Payload payloadAfterPayloadFinished)
            : base(cancellationTokenSource)
        {
            this.Payload = payload;
            this.PayloadAfterPayloadFinished = payloadAfterPayloadFinished;
        }

        readonly Payload Payload;
        bool PayloadFinished = false;
        object PayloadFinishedResult = null;
        readonly Payload PayloadAfterPayloadFinished;
        bool PayloadAfterPayloadFinishedFinsihed = false;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            if(PayloadFinished && PayloadAfterPayloadFinishedFinsihed)
            {
                return new PayloadResultSucceeded();
            }
            else
            {
                if(!PayloadFinished)
                {
                    var payloadResult = Payload.Run(parameter);
                    if(payloadResult.Status == PayloadResultStatus.Succeeded)
                    {
                        PayloadFinished = true;
                        PayloadFinishedResult = payloadResult.Result;
                        return new PayloadResultContinuationPending(payloadResult.Result);
                    }
                    else
                    {
                        return payloadResult;
                    }
                }
                else
                {
                    var payloadResult = PayloadAfterPayloadFinished.Run(PayloadFinishedResult);
                    if(payloadResult.Status == PayloadResultStatus.Succeeded)
                    {
                        PayloadAfterPayloadFinishedFinsihed = true;
                        return new PayloadResultContinuationPending(payloadResult.Result);
                    }
                    else
                    {
                        return payloadResult;
                    }
                }
            }
        }
    }
}