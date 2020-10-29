using System;

namespace compressor.Common.Threading
{
    class ThreadRunnerThreadPool: ThreadRunner
    {
        static System.Threading.WaitCallback WaitCallback(AsyncResult asyncResult, Action<object> runner)
        {
            return (state) => {
                try
                {
                    runner(state);
                }
                finally
                {
                    try
                    {
                        asyncResult.SetAsCompleted(false);
                    }
                    catch
                    {
                    }
                }
            };
        }

        public IAsyncResult QueueAndRun(Action<object> runner, object state)
        {
            if(runner == null)
            {
                throw new ArgumentNullException("runner");
            }

            var asyncResult = new AsyncResult(null, null);
            System.Threading.ThreadPool.QueueUserWorkItem(WaitCallback(asyncResult, runner), state);
            return asyncResult;
        }

        public IAsyncResult QueueAndRun(Action<object> runner, object state, string name)
        {
            if(runner == null)
            {
                throw new ArgumentNullException("runner");
            }
            
            var asyncResult = new AsyncResult(null, null);
            System.Threading.ThreadPool.QueueUserWorkItem(WaitCallback(asyncResult, (state) => {
                var oldName = System.Threading.Thread.CurrentThread.Name;
                try
                {
                    System.Threading.Thread.CurrentThread.Name = name;
                    runner(state);
                }
                finally
                {
                    System.Threading.Thread.CurrentThread.Name = oldName;
                }
            }), state);
            return asyncResult;
        }
    }
}