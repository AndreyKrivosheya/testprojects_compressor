using System;

namespace compressor.Common.Threading
{
    class ThreadRunnerSpawnThread: ThreadRunner
    {
        public void QueueAndRun(Action<object> runner, object state)
        {
            new System.Threading.Thread((state) => runner(state)) { IsBackground = true }.Start(state);
        }
    }
}