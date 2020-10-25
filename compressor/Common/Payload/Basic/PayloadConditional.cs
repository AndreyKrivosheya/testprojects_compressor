using System;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadConditional: Payload
    {
        public PayloadConditional(CancellationTokenSource cancellationTokenSource, Func<object, bool> condition, Payload payloadIfTure, Payload payloadIfFalse)
            : base(cancellationTokenSource)
        {
            this.Condition = condition;
            this.PayloadIfTrueCurrent = this.PayloadIfTrue = payloadIfTure;
            this.PayloadIfFalseCurrent = this.PayloadIfFalse = payloadIfFalse;
        }
        public PayloadConditional(CancellationTokenSource cancellationTokenSource, Func<object, bool> condition, Payload payloadIfTrue)
            : this(cancellationTokenSource, condition, payloadIfTrue, null)
        {
        }
        public PayloadConditional(CancellationTokenSource cancellationTokenSource, Func<bool> condition, Payload payloadIfTrue, Payload payloadIfFalse)
            : this(cancellationTokenSource, (obj) => condition(), payloadIfTrue, payloadIfFalse)
        {
        }
        public PayloadConditional(CancellationTokenSource cancellationTokenSource, Func<bool> condition, Payload payloadIfTrue)
            : this(cancellationTokenSource, condition, payloadIfTrue, null)
        {
        }

        readonly Func<object, bool> Condition;
        readonly Payload PayloadIfTrue;
        Payload PayloadIfTrueCurrent;
        readonly Payload PayloadIfFalse;
        Payload PayloadIfFalseCurrent;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            if(PayloadIfTrueCurrent == null && PayloadIfFalseCurrent == null)
            {
                return new PayloadResultSucceeded();
            }
            else
            {
                if(Condition(parameter))
                {
                    if(PayloadIfTrueCurrent != null)
                    {
                        var payloadResult = PayloadIfTrueCurrent.Run(parameter);
                        if(payloadResult.Status == PayloadResultStatus.Succeeded)
                        {
                            PayloadIfTrueCurrent = null;
                            return new PayloadResultContinuationPending(payloadResult.Result);
                        }
                        else
                        {
                            return payloadResult;
                        }
                    }
                    else
                    {
                        return new PayloadResultContinuationPendingDoneNothing();
                    }
                }
                else
                {
                    if(PayloadIfFalseCurrent != null)
                    {
                        var payloadResult = PayloadIfFalseCurrent.Run(parameter);
                        if(payloadResult.Status == PayloadResultStatus.Succeeded)
                        {
                            PayloadIfFalseCurrent = null;
                            return new PayloadResultContinuationPending(payloadResult.Result);
                        }
                        else
                        {
                            return payloadResult;
                        }
                    }
                    else
                    {
                        return new PayloadResultContinuationPendingDoneNothing();
                    }
                }
            }
        }
    }
}