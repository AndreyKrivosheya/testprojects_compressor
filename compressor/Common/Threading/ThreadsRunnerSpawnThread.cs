using System;

namespace compressor.Common.Threading
{
    class ThreadRunnerSpawnThread: ThreadRunner
    {
        static System.Threading.ParameterizedThreadStart ThreadStart(AsyncResult asyncResult, Action<object> runner)
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
            (new System.Threading.Thread(ThreadStart(asyncResult, runner)) { IsBackground = true }).Start(state);
            return asyncResult;
        }

        public IAsyncResult QueueAndRun(Action<object> runner, object state, string name)
        {
            if(runner == null)
            {
                throw new ArgumentNullException("runner");
            }
            
            var asyncResult = new AsyncResult(null, null);
            (new System.Threading.Thread(ThreadStart(asyncResult, runner)) { IsBackground = true, Name = name }).Start(state);
            return asyncResult;
        }
    }
}