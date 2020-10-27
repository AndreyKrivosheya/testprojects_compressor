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

        protected abstract PayloadResult RunUnsafe(object parameter);
        public PayloadResult Run(object parameter)
        {
            try
            {
                return RunUnsafe(parameter);
            }
            catch(OperationCanceledException e)
            {
                return new PayloadResultCanceled(e);
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

        protected virtual IEnumerable<WaitHandle> GetAllImmediateWaitHandlesForRepeatAwaiting()
        {
            return Enumerable.Empty<WaitHandle>();
        }
        protected IEnumerable<WaitHandle> GetAllWaitHandlesForRepeatAwaiting()
        {
            return GetThisAndAllSubpayloads().SelectMany(x => x.GetAllImmediateWaitHandlesForRepeatAwaiting());
        }

        protected virtual IEnumerable<Payload> GetAllImmediateSubpayloads()
        {
            return Enumerable.Empty<Payload>();
        }
        protected IEnumerable<Payload> GetThisAndAllSubpayloads()
        {
            static IEnumerable<Payload> GetAllSubpayloadsOfAPayload(Payload payload)
            {
                var subpayloads = payload.GetAllImmediateSubpayloads().Where(x => x != null);
                if(subpayloads.Any())
                {
                    return Enumerable.Concat(new [] { payload }, subpayloads.SelectMany(x => GetAllSubpayloadsOfAPayload(x)));
                }
                else
                {
                    return new [] { payload };
                }
            }

            return GetAllSubpayloadsOfAPayload(this);
        }
    }
}