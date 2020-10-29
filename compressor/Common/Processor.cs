using System;
using System.Threading;

using compressor.Common.Threading;

namespace compressor.Common
{
    class Processor
    {
        public Processor(Action actionToRun)
        {
            this.ActionToRun = actionToRun;
        }
        public Processor()
            : this(null)
        {
        }

        readonly Action ActionToRun;

        protected virtual void RunOnThread()
        {
            if(null != ActionToRun)
            {
                ActionToRun();
            }
        }

        AsyncResult PendingAsyncResult;
        
        public IAsyncResult BeginRun(AsyncCallback asyncCallback = null, object state = null)
        {
            var asyncResultNew = new AsyncResult(asyncCallback, state);
            if(Interlocked.CompareExchange(ref PendingAsyncResult, asyncResultNew, null) != null)
            {
                throw new InvalidOperationException("Only one asynchronius run request is allowed");
            }
            else
            {
                // spawn async execution
                Threads.QueueAndRun((object asyncResult) => {
                    var asyncResultTyped = (AsyncResult)asyncResult;
                    try
                    {
                        RunOnThread();
                        asyncResultTyped.SetAsCompleted(false);
                    }
                    catch(Exception e)
                    {
                        asyncResultTyped.SetAsCompletedFailed(e, false);
                    }
                }, PendingAsyncResult);
                
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
            var asyncResultNew = new AsyncResult(null, null);
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

    class Processor<T>
    {
        public Processor(Func<T> funcToRun)
        {
            this.FuncToRun = funcToRun;
        }

        public Processor()
            : this(null)
        {
        }

        readonly Func<T> FuncToRun;

        protected virtual T RunOnThread()
        {
            if(FuncToRun != null)
            {
                return FuncToRun();
            }
            else
            {
                return default(T);
            }
        }

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
                // spawn async execution
                Threads.QueueAndRun((object asyncResult) => {
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
                }, PendingAsyncResult);

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