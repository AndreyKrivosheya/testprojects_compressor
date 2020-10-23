using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Common;
using compressor.Processor.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class ProcessorParallel: Processor
    {
        public ProcessorParallel(SettingsProvider settings, Stream inputStream, Stream outputStream, Factory payloadFactory)
            : base(settings, inputStream, outputStream)
        {
            this.PayloadFactory = payloadFactory;
        }

        readonly Factory PayloadFactory;
        
        protected sealed override void RunOnThread()
        {
            var queueSize = Settings.MaxQueueSize;
            var queueToProcess = new QueueToProcess(queueSize);
            var queueToWrite = new QueueToWrite(queueSize);

            var concurrency = Settings.MaxConcurrency;
            var threads = new Thread[concurrency - 1];
            var threadsPayloads = new Payload.Payload[concurrency - 1];
            var threadsErrors = new ExceptionDispatchInfo[concurrency - 1];

            // spawn processors, if needed
            for(int i = 0; i < concurrency - 1; i++)
            {
                threadsPayloads[i] = PayloadFactory.CreateCompressDecompress(Settings);
                threads[i] = new Thread((object task) => {
                    try
                    {
                        ((Payload.Payload)task).Run(queueToProcess, queueToWrite);
                    }
                    catch(Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                        threadsErrors[i] = ExceptionDispatchInfo.Capture(e);
                    }
                }) { IsBackground = true, Name = string.Format("Processor[{0}]: Process worker", i) };
                threads[i].Start(threadsPayloads[i]);
            }
            // run reader/processor/writer
            try
            {
                PayloadFactory.CreateReadCompressDecompressWrite(Settings, InputStream, OutputStream, threads).Run(queueToProcess, queueToWrite);
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
    }
}