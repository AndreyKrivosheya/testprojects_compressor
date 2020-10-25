using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadConditionalOnceAndForever: PayloadConditional
    {
        public PayloadConditionalOnceAndForever(CancellationTokenSource cancellationTokenSource, Func<object, bool> condition, Payload payloadIfTrue)
            : base(cancellationTokenSource, (new ConditionWithResultsPersisted(condition)).Evaluate, payloadIfTrue)
        {
        }
        public PayloadConditionalOnceAndForever(CancellationTokenSource cancellationTokenSource, Func<bool> condition, Payload payloadIfTrue)
            : this(cancellationTokenSource, (parameter) => condition(), payloadIfTrue)
        {
        }

        class ConditionWithResultsPersisted
        {
            public ConditionWithResultsPersisted(Func<object, bool> condition)
            {
                this.Condition = condition;
            }

            readonly Func<object, bool> Condition;

            bool AlreadyEvaluatedToTrue = false;

            public bool Evaluate(object parameter)
            {
                if(AlreadyEvaluatedToTrue)
                {
                    return true;
                }
                else
                {
                    if(Condition(parameter))
                    {
                        AlreadyEvaluatedToTrue = true;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}