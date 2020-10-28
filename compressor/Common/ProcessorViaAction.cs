using System;

namespace compressor.Common
{
    class ProcessorViaAction : Processor
    {
        public ProcessorViaAction(Action actionToRun)
        {
            if(actionToRun == null)
            {
                throw new ArgumentNullException("actionToRun");
            }

            this.ActionToRun = actionToRun;
        }

        readonly Action ActionToRun;

        protected sealed override void RunOnThread()
        {
            ActionToRun();
        }
    }
}