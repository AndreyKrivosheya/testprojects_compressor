using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class Payload
    {
        public Payload(SettingsProvider settings)
        {
            this.Settings = settings;
        }

        protected readonly SettingsProvider Settings;

        protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }
        protected virtual void RunIdleYeild()
        {
            Thread.Yield();
        }
        protected virtual void RunIdleSleep(int milliseconds)
        {
            RunIdleSleep(milliseconds, new WaitHandle[] {});
        }
        protected void RunIdleSleep(int milliseconds, IEnumerable<WaitHandle> waitables)
        {
            WaitHandle.WaitAny(new WaitHandle[] { CancellationTokenSource.Token.WaitHandle }.Concat(waitables).Where(x => x != null).ToArray(), milliseconds);
        }
        protected virtual void RunIdleSleep(int milliseconds, IEnumerable<IAsyncResult> waitables)
        {
            RunIdleSleep(milliseconds, waitables.Where(x => x != null).Select(x => x.AsyncWaitHandle));
        }
        protected virtual void RunIdleSleep()
        {
            RunIdleSleep(100);
        }
        public virtual void Run(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            while(!CancellationTokenSource.IsCancellationRequested)
            {
                var result = RunOnce(queueToProcess, queueToWrite);
                // if anything happened, and run cycle wasn't empty
                if(result.HasValue)
                {
                    // if runcycle competed task
                    if(result.Value)
                    {
                        break;
                    }
                    else
                    {
                        // to the next runcycle
                        RunIdleYeild();
                    }
                }
                else
                {
                    // be gentle with the CPU, don't waist all onto checking if there's nothing to do
                    RunIdleSleep();
                }
            }
        }
        public virtual bool? RunOnce(QueueToProcess quequeToProcess, QueueToWrite queueToWrite)
        {
            return true;
        }
    }
}