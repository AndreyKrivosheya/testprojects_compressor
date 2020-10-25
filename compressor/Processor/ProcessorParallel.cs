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
            var cancellation = new CancellationTokenSource();
            var threads = new Thread[concurrency - 1];
            var threadsPayloads = new Common.Payload.Payload[concurrency - 1];
            var threadsErrors = new ExceptionDispatchInfo[concurrency - 1];

            // spawn processors, if needed
            for(int i = 0; i < concurrency - 1; i++)
            {
                //threadsPayloads[i] = PayloadFactory.CreateCompressDecompress(Settings);
                threadsPayloads[i] = (new Payload2.PayloadFactoryCompress(cancellation, Settings)).CreateProcess(queueToProcess, queueToWrite);
                threads[i] = new Thread((object idxRaw) => {
                    var idx = (int)idxRaw;
                    try
                    {
                        var payloadResult = threadsPayloads[idx].Run();
                        if(payloadResult.Status == Common.Payload.PayloadResultStatus.Failed)
                        {
                            threadsErrors[idx] = ((Common.Payload.PayloadResultFailed)payloadResult).Failure;
                        }
                    }
                    catch(Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                        threadsErrors[idx] = ExceptionDispatchInfo.Capture(e);
                    }
                }) { IsBackground = true, Name = string.Format("Processor[{0}]: Process worker", i) };
                threads[i].Start(i);
            }
            // run reader/processor/writer
            try
            {
                //PayloadFactory.CreateReadCompressDecompressWrite(Settings, InputStream, OutputStream, threads).Run(queueToProcess, queueToWrite);
                var payload = (new Payload2.PayloadFactoryCompress(cancellation, Settings)).CreateReadProcessWrite(InputStream, OutputStream, queueToProcess, queueToWrite, threads);
                var payloadResult = payload.Run();
                if(payloadResult.Status == Common.Payload.PayloadResultStatus.Failed)
                {
                    ((Common.Payload.PayloadResultFailed)payloadResult).Failure.Throw();
                }
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