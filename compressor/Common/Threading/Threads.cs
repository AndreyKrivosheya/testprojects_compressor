using System;

namespace compressor.Common.Threading
{
    static class Threads
    {
        static readonly ThreadRunner Runner = new ThreadRunnerSpawnThread();

        public static IAsyncResult QueueAndRun(Action<object> runner, object state)
        {
            return Runner.QueueAndRun(runner, state);
        }

        public static IAsyncResult QueueAndRun(Action<object> runner, object state, string name)
        {
            return Runner.QueueAndRun(runner, state, name);
        }
    }
}