using System;
using System.Collections.Generic;

namespace compressor
{
    class StepsRunner
    {
        public class Step
        {
            public Step(Func<bool> condition, Func<ProcessorQueueToProcess, ProcessorQueueToWrite, bool?> executor)
            {
                this.Condition = condition;
                this.Executor = executor;
            }

            public Step(Func<ProcessorQueueToProcess, ProcessorQueueToWrite, bool?> executor)
                : this(() => true, executor)
            {
            }

            public readonly Func<bool> Condition;
            public readonly Func<ProcessorQueueToProcess, ProcessorQueueToWrite, bool?> Executor;
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
        public bool? Run(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite)
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