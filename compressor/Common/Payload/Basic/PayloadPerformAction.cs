using System;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadPerformAction: Payload
    {
        public PayloadPerformAction(CancellationTokenSource cancellationTokenSource, Action<object> actionToPerform)
            : base(cancellationTokenSource)
        {
            this.ActionToPerform = actionToPerform;
            if(this.ActionToPerform == null)
            {
                this.ActionToPerform = (parameter) => {};
            }
        }
        public PayloadPerformAction(CancellationTokenSource cancellationTokenSource, Action actionToPerform)
            : this(cancellationTokenSource, actionToPerform == null ? ((Action<object>)((parameter) => {})) : ((Action<object>)((paramter) => { actionToPerform(); })))
        {
        }

        readonly Action<object> ActionToPerform;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            ActionToPerform(parameter);
            return new PayloadResultContinuationPending();
        }
    }
}