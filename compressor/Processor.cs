using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Common;

namespace compressor
{
    class Processor<TaskFactoryReadWrite, TaskFactoryCompressDecompress>
        where TaskFactoryReadWrite: IProcessorTaskFactoryReadWrite, new()
        where TaskFactoryCompressDecompress: IProcessorTaskFactoryCompressDecompress, new()
    {
        public Processor(ISettingsProvider settings, Stream inputStream, Stream outputStream)
        {
            if(settings == null)
            {
                this.Settings = new SettingsProviderFromEnvironment();
            }
            else
            {
                this.Settings = settings;
            }

            if(inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }
            this.InputStream = inputStream;

            if(outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }
            this.OutputStream = outputStream;
        }

        private readonly ISettingsProvider Settings;

        private readonly Stream InputStream;
        private readonly Stream OutputStream;

        void RunOnThread()
        {
            var queueSize = Settings.MaxQueueSize;
            var queueToProcess = new ProcessorQueueToProcess(queueSize);
            var queueToWrite = new ProcessorQueueToWrite(queueSize);

            var concurrency = Settings.MaxConcurrency;
            var threads = new Thread[concurrency - 1];
            var threadsPayloads = new ProcessorTask[concurrency - 1];
            var threadsErrors = new ExceptionDispatchInfo[concurrency - 1];

            // spawn processors, if needed
            for(int i = 0; i < concurrency - 1; i++)
            {
                threadsPayloads[i] = (new TaskFactoryCompressDecompress()).Create(Settings);
                threads[i] = new Thread((object task) => {
                    var taskTyped = task as ProcessorTask;
                    if(taskTyped != null)
                    {
                        try
                        {
                            taskTyped.Run(queueToProcess, queueToWrite);
                        }
                        catch(Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e);
                            threadsErrors[i] = ExceptionDispatchInfo.Capture(e);
                        }
                    }
                }) { IsBackground = true, Name = string.Format("Processor[{0}]: Process worker", i) };
                threads[i].Start(threadsPayloads[i]);
            }
            // run reader/processor/writer
            try
            {
                (new TaskFactoryReadWrite()).Create(Settings, InputStream, OutputStream, threads).Run(queueToProcess, queueToWrite);
            }
            catch(Exception)
            {
                // abort processors payloads
                for(var i = 0; i < concurrency - 1; i++)
                {
                    threadsPayloads[i].Cancel();
                    threads[i].Join();
                }
                
                throw;
            }
            // wait processors are all finshed (should already be)
            for(int i = 0; i < concurrency - 1; i++)
            {
                threads[i].Join();
            }
            // ... report exceptions if any
            var errors = threadsErrors.Where(x => x != null);
            if(errors.Any())
            {
                var exceptions = errors.Select(x => {
                        try
                        {
                            x.Throw();
                            return null;
                        }
                        catch(Exception e)
                        {
                            return e;
                        }
                    });
                throw new AggregateException("Failed to process", exceptions);
            }
        }

        AsyncResultNoResult PendingAsyncResult;
        public IAsyncResult BeginRun(AsyncCallback asyncCallback, object state)
        {
            var asyncResultNew = new AsyncResultNoResult(asyncCallback, state);
            if(Interlocked.CompareExchange(ref PendingAsyncResult, asyncResultNew, null) != null)
            {
                throw new InvalidOperationException("Only one asynchronius run request is allowed");
            }
            else
            {
                (new Thread((object asyncResult) => {
                    var asyncResultTyped = (AsyncResultNoResult)asyncResult;
                    if(asyncResult != null)
                    {
                        try
                        {
                            RunOnThread();
                            asyncResultTyped.SetAsCompleted(null, false);
                        }
                        catch(Exception e)
                        {
                            asyncResultTyped.SetAsCompleted(e, false);
                        }
                    }
                }) { IsBackground = true }).Start(PendingAsyncResult);
                return PendingAsyncResult;
            }
        }
        public void EndRun(IAsyncResult asyncResult)
        {
            if(asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            // assumes only one thread is calling this function
            if(PendingAsyncResult == null)
            {
                throw new InvalidOperationException("No asynchronious runs were requested");
            }
            else
            {
                if(!object.ReferenceEquals(asyncResult, PendingAsyncResult))
                {
                    throw new InvalidOperationException("End of asynchronius run request did not originate from a BeginRun() method on the current processor");
                }
                else
                {
                    PendingAsyncResult.EndInvoke();
                    PendingAsyncResult = null;
                }
            }
        }

        public void Run()
        {
            var asyncResultNew = new AsyncResultNoResult(null, null);
            if(Interlocked.CompareExchange(ref PendingAsyncResult, asyncResultNew, null) != null)
            {
                throw new InvalidOperationException("Running synchroniously and asynchroniously simulteneously is not allowed");
            }
            else
            {
                try
                {
                    RunOnThread();
                }
                finally
                {
                    PendingAsyncResult = null;
                }
            }
        }
    }
}