using System;

namespace compressor.Common.Threading
{
    static class Threads
    {
        static readonly ThreadRunner Runner = new ThreadRunnerSpawnThread();

        public static void QueueAndRun(Action<object> runner, object state)
        {
            Runner.QueueAndRun(runner, state);
        }
    }
}