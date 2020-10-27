using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadConditional: Payload, AwaitablesHolder
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
                        return new PayloadResultContinuationPendingEvaluatedToEmptyPayload();
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
                        return new PayloadResultContinuationPendingEvaluatedToEmptyPayload();
                    }
                }
            }
        }
         
        #region AwaitablesHolder implementation

        IEnumerable<WaitHandle> AwaitablesHolder.GetAwaitables()
        {
            if(PayloadIfTrue != null && PayloadIfFalse != null)
            {
                return Enumerable.Concat(
                    PayloadIfTrue.GetAwaitables(),
                    PayloadIfFalse.GetAwaitables()
                );
            }
            else if(PayloadIfTrue != null)
            {
                return PayloadIfTrue.GetAwaitables();
            }
            else if(PayloadIfFalse != null)
            {
                return PayloadIfFalse.GetAwaitables();
            }
            else
            {
                return Enumerable.Empty<WaitHandle>();
            }
        }

        #endregion
    }
}