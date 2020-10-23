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
        protected void RunIdleSleep(int milliseconds, IEnumerable<WaitHandle> waitables)
        {
            WaitHandle.WaitAny(new WaitHandle[] { CancellationTokenSource.Token.WaitHandle }.Concat(waitables).Where(x => x != null).ToArray(), milliseconds);
        }
        protected virtual void RunIdleSleep(int milliseconds, IEnumerable<IAsyncResult> waitables)
        {
            RunIdleSleep(milliseconds, waitables.Where(x => x != null).Select(x => x.AsyncWaitHandle));
        }
        protected void RunIdleSleep()
        {
            RunIdleSleep(1000, Enumerable.Empty<IAsyncResult>());
        }

        protected enum RunOnceResult
        {
            Finished,
            WorkDoneButNotFinished,
            DoneNothing,
        };
        protected virtual RunOnceResult RunOnce(QueueToProcess quequeToProcess, QueueToWrite queueToWrite)
        {
            return RunOnceResult.Finished;
        }
        public virtual void Run(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            var finished = false;
            while(!finished && !CancellationTokenSource.IsCancellationRequested)
            {
                switch(RunOnce(queueToProcess, queueToWrite))
                {
                    case RunOnceResult.Finished:
                        // runcycle competed payload
                        finished = true;
                        break;
                    case RunOnceResult.WorkDoneButNotFinished:
                        // some work was done, but that doesn't completed payload
                        // ... to the next runcycle
                        RunIdleYeild();
                        break;
                    case RunOnceResult.DoneNothing:
                        // spent the cycle checking if anything is ready to work on
                        // ... be gentle with the CPU, don't waste all onto checking if there's nothing to do
                        RunIdleSleep();
                        break;
                }
            }
        }
    }
}