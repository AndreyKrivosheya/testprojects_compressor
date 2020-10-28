using System;

namespace compressor.Common.Threading
{
    class ThreadRunnerThreadPool: ThreadRunner
    {
        public void QueueAndRun(Action<object> runner, object state)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((state) => runner(state), state);
        }
    }
}