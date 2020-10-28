using System;

namespace compressor.Common.Threading
{
    interface ThreadRunner
    {
        void QueueAndRun(Action<object> runner, object state);
    }
}