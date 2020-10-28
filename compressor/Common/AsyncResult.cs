// Refactored from https://docs.microsoft.com/en-us/archive/msdn-magazine/2007/march/implementing-the-clr-asynchronous-programming-model by Jeffrey Richter

using System;

namespace compressor.Common
{
    class AsyncResult<TResult> : AsyncResultNoResult
    {
        // Field set when operation completes
        TResult m_result = default(TResult);

        public AsyncResult(AsyncCallback asyncCallback, Object state) : 
            base(asyncCallback, state)
        {
        }

        public void SetAsCompleted(TResult result, Boolean completedSynchronously)
        {
            // Save the asynchronous operation's result
            m_result = result;
            // Tell the base class that the operation completed sucessfully (no exception)
            base.SetAsCompleted(completedSynchronously);
        }

        new public TResult EndInvoke()
        {
            // Wait until operation has completed 
            base.EndInvoke();
            // Return the result (if above didn't throw)
            return m_result;
        }
    }
}