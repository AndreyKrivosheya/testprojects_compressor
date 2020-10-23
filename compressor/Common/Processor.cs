using System;
using System.Threading;

namespace compressor.Common
{
    abstract class Processor
    {
        public Processor()
        {
        }

        protected abstract void RunOnThread();

        AsyncResultNoResult PendingAsyncResult;
        public IAsyncResult BeginRun(AsyncCallback asyncCallback, object state)
        {
            var asyncResultNew = new AsyncResultNoResult(asyncCallback, state);
            if(Interlocked.CompareExchange(ref PendingAsyncResult, asyncResultNew, null) != null)
            {
                throw new InvalidOperationException("Only one asynchronius run request is allowed");
            }
            else
            {
                (new Thread((object asyncResult) => {
                    var asyncResultTyped = (AsyncResultNoResult)asyncResult;
                    if(asyncResult != null)
                    {
                        try
                        {
                            RunOnThread();
                            asyncResultTyped.SetAsCompleted(null, false);
                        }
                        catch(Exception e)
                        {
                            asyncResultTyped.SetAsCompleted(e, false);
                        }
                    }
                }) { IsBackground = true }).Start(PendingAsyncResult);
                return PendingAsyncResult;
            }
        }
        public void EndRun(IAsyncResult asyncResult)
        {
            if(asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            // assumes only one thread is calling this function
            if(PendingAsyncResult == null)
            {
                throw new InvalidOperationException("No asynchronious runs were requested");
            }
            else
            {
                if(!object.ReferenceEquals(asyncResult, PendingAsyncResult))
                {
                    throw new InvalidOperationException("End of asynchronius run request did not originate from a BeginRun() method on the current processor");
                }
                else
                {
                    PendingAsyncResult.EndInvoke();
                    PendingAsyncResult = null;
                }
            }
        }

        public void Run()
        {
            var asyncResultNew = new AsyncResultNoResult(null, null);
            if(Interlocked.CompareExchange(ref PendingAsyncResult, asyncResultNew, null) != null)
            {
                throw new InvalidOperationException("Running synchroniously and asynchroniously simulteneously is not allowed");
            }
            else
            {
                try
                {
                    RunOnThread();
                }
                finally
                {
                    PendingAsyncResult = null;
                }
            }
        }
    }
}