using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadChain: Payload
    {
        public PayloadChain(CancellationTokenSource cancellationTokenSource, bool shouldConvertSucceededToPending, IEnumerable<Payload> payloads)
            : base(cancellationTokenSource)
        {
            this.ShouldConvertSuceededToPending = shouldConvertSucceededToPending;
            this.Payloads = payloads.ToArray();
        }
        public PayloadChain(CancellationTokenSource cancellationTokenSource, bool shouldConvertSucceededToPending, params Payload[] payloads)
            : this(cancellationTokenSource, shouldConvertSucceededToPending, payloads.AsEnumerable())
        {
        }

        readonly bool ShouldConvertSuceededToPending;
        readonly IEnumerable<Payload> Payloads;
        
        IEnumerator<Payload> PayloadCurrent = null;
        object PayloadCurrentParameter = null;

        protected override IEnumerable<Common.Payload.Payload> GetCurrentSubpayloadsForThreadsSleep()
        {
            return Payloads;
        }
        protected override IEnumerable<WaitHandle> GetCurrentWaitHandlesForThreadSleep()
        {
            var baseAwaitables = base.GetCurrentWaitHandlesForThreadSleep();
            if(PayloadCurrentParameter != null)
            {
                var payloadCurrentParameterAsSyncResult = PayloadCurrentParameter as IAsyncResult;
                if(payloadCurrentParameterAsSyncResult != null)
                {
                    return baseAwaitables.Concat(new [] { payloadCurrentParameterAsSyncResult.AsyncWaitHandle });
                }

                var payloadCurrentParameterAsWaitHandle = PayloadCurrentParameter as WaitHandle;
                if(payloadCurrentParameterAsWaitHandle != null)
                {
                    return baseAwaitables.Concat(new [] { payloadCurrentParameterAsWaitHandle });
                }
            }

            return baseAwaitables;
        }

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
                        if(ShouldConvertSuceededToPending)
                        {
                            return new PayloadResultContinuationPending(payloadCurrentResult.Result);
                        }
                        else
                        {
                            return payloadCurrentResult;
                        }
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