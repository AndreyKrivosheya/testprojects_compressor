using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

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
            public ThreadContext(ProcessorQueueToProcess queueToProcess, ProcessorQueueToWrite queueToWrite, Thread thread, ProcessorTask task)
            {
                this.QueueToProcess = queueToProcess;
                this.QueueToWrite = queueToWrite;
                this.Thread = thread;
                this.Task = task;
            }

            public readonly ProcessorQueueToProcess QueueToProcess;
            public readonly ProcessorQueueToWrite QueueToWrite;

            public readonly Thread Thread;
            public readonly ProcessorTask Task;
            public ExceptionDispatchInfo ThreadExecutionException;
        }
        public void Run()
        {
            var queueSize = Settings.MaxQueueSize;
            var queueToProcess = new ProcessorQueueToProcess(queueSize);
            var queueToWrite = new ProcessorQueueToWrite(queueSize);

            var concurrency = Settings.MaxConcurrency;
            var threads = new ThreadContext[concurrency];

            for(int i = 0; i < threads.Length; i++)
            {
                if(i == 0)
                {
                    threads[i] = new ThreadContext(queueToProcess, queueToWrite, new Thread(RunTask), (new TaskFactoryReadWrite()).Create(Settings, InputStream, OutputStream, threads.Skip(1).Select(x => x.Thread)));
                    threads[i].Thread.Name = string.Format("Processor[{0}]: Read/Process/Write worker", i);
                }
                else
                {
                    threads[i] = new ThreadContext(queueToProcess, queueToWrite, new Thread(RunTask) { IsBackground = true }, (new TaskFactoryCompressDecompress()).Create(Settings));
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

        private void RunTask(object ctx)
        {
            var ctxTyped = ctx as ThreadContext;
            if(ctxTyped != null)
            {
                RunTask(ctxTyped);
            }
        }
        private void RunTask(ThreadContext ctx)
        {
            try
            {
                if(ctx.Task != null)
                    ctx.Task.Run(ctx.QueueToProcess, ctx.QueueToWrite);
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                ctx.ThreadExecutionException = ExceptionDispatchInfo.Capture(e);
            }
        }
    }
}