using System;

namespace compressor.Common.Threading
{
    interface ThreadRunner
    {
        IAsyncResult QueueAndRun(Action<object> runner, object state);

        IAsyncResult QueueAndRun(Action<object> runner, object state, string name);
    }
}