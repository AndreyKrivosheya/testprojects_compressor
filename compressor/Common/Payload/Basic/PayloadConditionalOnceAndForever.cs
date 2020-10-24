using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadConditionalOnceAndForever: PayloadConditional
    {
        public PayloadConditionalOnceAndForever(CancellationTokenSource cancellationTokenSource, Func<object, bool> condition, Payload payloadIfTrue)
            : base(cancellationTokenSource, (new ConditionResultPersister(condition)).Evaluate, payloadIfTrue)
        {
        }
        public PayloadConditionalOnceAndForever(CancellationTokenSource cancellationTokenSource, Func<bool> condition, Payload payloadIfTrue)
            : this(cancellationTokenSource, (parameter) => condition(), payloadIfTrue)
        {
        }

        class ConditionResultPersister
        {
            public ConditionResultPersister(Func<object, bool> condition)
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
                        return AlreadyEvaluatedToTrue = true;
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