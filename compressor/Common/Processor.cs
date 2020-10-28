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
        public IAsyncResult BeginRun(AsyncCallback asyncCallback = null, object state = null)
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
                    try
                    {
                        RunOnThread();
                        asyncResultTyped.SetAsCompleted(false);
                    }
                    catch(Exception e)
                    {
                        asyncResultTyped.SetAsCompletedFailed(e, false);
                    }
                }) { IsBackground = true }).Start(PendingAsyncResult);
                // ThreadPool.QueueUserWorkItem((asyncResult) => {
                //     var asyncResultTyped = (AsyncResultNoResult)asyncResult;
                //     if(asyncResultTyped != null)
                //     {
                //         try
                //         {
                //             RunOnThread();
                //             asyncResultTyped.SetAsCompleted(false);
                //         }
                //         catch(Exception e)
                //         {
                //             asyncResultTyped.SetAsCompletedFailed(e, false);
                //         }
                //     }
                // }, PendingAsyncResult);
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
                    try
                    {
                        PendingAsyncResult.EndInvoke();
                    }
                    finally
                    {
                        PendingAsyncResult = null;
                    }
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