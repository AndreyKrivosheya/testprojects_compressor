using System;
using System.Threading;

namespace compressor.Common
{
    abstract class ProcessorWithResult<T>
    {
        public ProcessorWithResult()
        {
        }

        protected abstract T RunOnThread();

        AsyncResult<T> PendingAsyncResult;
        public IAsyncResult BeginRun(AsyncCallback asyncCallback = null, object state = null)
        {
            var asyncResultNew = new AsyncResult<T>(asyncCallback, state);
            if(Interlocked.CompareExchange(ref PendingAsyncResult, asyncResultNew, null) != null)
            {
                throw new InvalidOperationException("Only one asynchronius run request is allowed");
            }
            else
            {
                (new Thread((object asyncResult) => {
                    var asyncResultTyped = (AsyncResult<T>)asyncResult;
                    try
                    {
                        var result = RunOnThread();
                        asyncResultTyped.SetAsCompleted(result, false);
                    }
                    catch(Exception e)
                    {
                        asyncResultTyped.SetAsCompletedFailed(e, false);
                    }
                }) { IsBackground = true }).Start(PendingAsyncResult);
                // ThreadPool.QueueUserWorkItem((object asyncResult) => {
                //     var asyncResultTyped = (AsyncResult<T>)asyncResult;
                //     try
                //     {
                //         var result = RunOnThread();
                //         asyncResultTyped.SetAsCompleted(result, false);
                //     }
                //     catch(Exception e)
                //     {
                //         asyncResultTyped.SetAsCompletedFailed(e, false);
                //     }
                // }, PendingAsyncResult);
                return PendingAsyncResult;
            }
        }
        public T EndRun(IAsyncResult asyncResult)
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
                        return PendingAsyncResult.EndInvoke();
                    }
                    finally
                    {
                        PendingAsyncResult = null;
                    }
                }
            }
        }

        public T Run()
        {
            var asyncResultNew = new AsyncResult<T>(null, null);
            if(Interlocked.CompareExchange(ref PendingAsyncResult, asyncResultNew, null) != null)
            {
                throw new InvalidOperationException("Running synchroniously and asynchroniously simulteneously is not allowed");
            }
            else
            {
                try
                {
                    return RunOnThread();
                }
                finally
                {
                    PendingAsyncResult = null;
                }
            }
        }
    }
}