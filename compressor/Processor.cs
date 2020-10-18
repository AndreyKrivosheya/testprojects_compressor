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

        private class ThreadContext
        {
            public ThreadContext(Thread thread, ProcessorTask task)
            {
                this.Thread = thread;
                this.Task = task;
            }

            public readonly Thread Thread;
            public readonly ProcessorTask Task;
            public ExceptionDispatchInfo ThreadExecutionException;
        }
        
        void RunOnThread()
        {
            var queueSize = Settings.MaxQueueSize;
            var queueToProcess = new ProcessorQueueToProcess(queueSize);
            var queueToWrite = new ProcessorQueueToWrite(queueSize);

            var concurrency = Settings.MaxConcurrency;
            var threads = new ThreadContext[concurrency];

            for(int i = 0; i < threads.Length; i++)
            {
                var thread = new Thread((object ctx) => {
                    var ctxTyped = ctx as ThreadContext;
                    if(ctxTyped != null)
                    {
                        try
                        {
                            if(ctxTyped.Task != null)
                            {
                                ctxTyped.Task.Run(queueToProcess, queueToWrite);
                            }
                        }
                        catch(Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e);
                            ctxTyped.ThreadExecutionException = ExceptionDispatchInfo.Capture(e);
                        }
                    }
                }) { IsBackground = true };

                if(i == 0)
                {
                    threads[i] = new ThreadContext(thread, (new TaskFactoryReadWrite()).Create(Settings, InputStream, OutputStream, threads.Skip(1).Select(x => x.Thread)));
                    threads[i].Thread.Name = string.Format("Processor[{0}]: Read/Process/Write worker", i);
                }
                else
                {
                    threads[i] = new ThreadContext(thread, (new TaskFactoryCompressDecompress()).Create(Settings));
                    threads[i].Thread.Name = string.Format("Processor[{0}]: Process worker", i);
                }
                
                threads[i].Thread.Start(threads[i]);
            }
            for(int i = 0; i < threads.Length; i++)
            {
                threads[i].Thread.Join();
                if(i == 0)
                {
                    // if reader/writer thread failed
                    if(threads[i].ThreadExecutionException != null)
                    {
                        // abort other threads
                        foreach(var thrd in threads.Skip(1))
                        {
                            thrd.Task.Cancel();
                            thrd.Thread.Join();
                            break;
                        }
                        // throw reader/writer thread failure
                        try
                        {
                            threads[i].ThreadExecutionException.Throw();
                        }
                        catch(Exception e)
                        {
                            throw new ApplicationException("Failed to process", e);
                        }
                    }
                }    
            }
            
            var errors = threads.Where(x => x.ThreadExecutionException != null).Select(x => x.ThreadExecutionException);
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
            EndRun(BeginRun(null, null));
        }
    }
}