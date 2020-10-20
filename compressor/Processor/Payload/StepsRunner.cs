using System;
using System.Collections.Generic;

using compressor.Processor.Queue;

namespace compressor.Processor.Payload
{
    class StepsRunner
    {
        public class Step
        {
            public Step(Func<bool> condition, Func<QueueToProcess, QueueToWrite, bool?> executor)
            {
                this.Condition = condition;
                this.Executor = executor;
            }

            public Step(Func<QueueToProcess, QueueToWrite, bool?> executor)
                : this(() => true, executor)
            {
            }

            public readonly Func<bool> Condition;
            public readonly Func<QueueToProcess, QueueToWrite, bool?> Executor;
        }

        public StepsRunner(params Step[] steps)
            : this((IEnumerable<Step>)steps)
        {
        }
        public StepsRunner(IEnumerable<Step> steps)
        {
            this.Steps = steps;
        }

        IEnumerable<Step> Steps;
        public bool? Run(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            foreach(var step in Steps)
            {
                if(step.Condition())
                {
                    return step.Executor(queueToProcess, queueToWrite);
                }
            }

            return null;
        }
    } 
}