using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace compressor.Common.Payload
{
    abstract class Payload
    {
        public Payload(CancellationTokenSource cancellationTokenSource)
        {
            this.CancellationTokenSource = cancellationTokenSource;
        }

        protected readonly CancellationTokenSource CancellationTokenSource;
        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }

        protected virtual IEnumerable<WaitHandle> GetAllCurrentWaitHandlesForThreadSleep()
        {
            return Enumerable.Empty<WaitHandle>();
        }

        protected abstract PayloadResult RunUnsafe(object parameter);
        public PayloadResult Run(object parameter)
        {
            try
            {
                return RunUnsafe(parameter);
            }
            catch(Exception e)
            {
                return new PayloadResultFailed(e);
            }
        }
        public PayloadResult Run()
        {
            return Run(null);
        }
    }
}