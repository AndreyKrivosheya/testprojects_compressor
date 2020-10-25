using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadConditional: Payload
    {
        public PayloadConditional(CancellationTokenSource cancellationTokenSource, Func<object, bool> condition, Payload payloadIfTrue = null, Payload payloadIfFalse = null)
            : base(cancellationTokenSource)
        {
            this.Condition = condition;
            this.PayloadIfTrue = payloadIfTrue;
            this.PayloadIfFalse = payloadIfFalse;
        }
        public PayloadConditional(CancellationTokenSource cancellationTokenSource, Func<bool> condition, Payload payloadIfTrue = null, Payload payloadIfFalse = null)
            : this(cancellationTokenSource, (parameter) => condition(), payloadIfTrue, payloadIfFalse)
        {
        }

        readonly Func<object, bool> Condition;
        readonly Payload PayloadIfTrue;
        readonly Payload PayloadIfFalse;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            if(PayloadIfTrue == null && PayloadIfFalse == null)
            {
                return new PayloadResultSucceeded();
            }
            else
            {
                if(Condition(parameter))
                {
                    if(PayloadIfTrue != null)
                    {
                        return PayloadIfTrue.Run(parameter);
                    }
                    else
                    {
                        return new PayloadResultContinuationPendingDoneNothing();
                    }
                }
                else
                {
                    if(PayloadIfFalse != null)
                    {
                        return PayloadIfFalse.Run(parameter);
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