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

        protected virtual IEnumerable<Payload> GetCurrentSubpayloadsForThreadsSleep()
        {
            return Enumerable.Empty<Payload>();
        }
        IEnumerable<Payload> GetAllSubpayloadsForThreadsSleep()
        {
            static IEnumerable<Payload> GetAllSubpayloadsOfAPayload(Payload payload)
            {
                var subPayloads = payload.GetCurrentSubpayloadsForThreadsSleep().Where(x => x != null);
                if(!subPayloads.Any())
                {
                    return new [] { payload };
                }
                else
                {
                    return new [] { payload }.Concat(subPayloads.SelectMany( x => GetAllSubpayloadsOfAPayload(x)));
                }
            }

            return GetAllSubpayloadsOfAPayload(this); 
        }

        protected virtual IEnumerable<WaitHandle> GetCurrentWaitHandlesForThreadSleep()
        {
            return Enumerable.Empty<WaitHandle>();
        }

        protected IEnumerable<WaitHandle> GetAllWaitHandlesForThreadsSleep()
        {
            return GetAllSubpayloadsForThreadsSleep().SelectMany(x => x.GetCurrentWaitHandlesForThreadSleep().Where(x => x != null));
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