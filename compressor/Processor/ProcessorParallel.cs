using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class ProcessorParallel: Processor
    {
        public ProcessorParallel(SettingsProvider settings, Stream inputStream, Stream outputStream, Func<CancellationTokenSource, SettingsProvider, PayloadFactory> payloadFactory)
            : base(settings, inputStream, outputStream)
        {
            this.CancellationTokenSource = new CancellationTokenSource();
            this.PayloadFactory = payloadFactory(CancellationTokenSource, Settings);
        }

        protected readonly CancellationTokenSource CancellationTokenSource;

        protected readonly PayloadFactory PayloadFactory;
        
        protected sealed override void RunOnThread()
        {
            var queueSize = Settings.MaxQueueSize;
            var queueToProcess = new QueueToProcess(queueSize);
            var queueToWrite = new QueueToWrite(queueSize);

            var concurrency = Settings.MaxConcurrency - 1;
            var threads = new Thread[concurrency];
            var threadsPayloads = new Common.Payload.Payload[concurrency];
            var threadsErrors = new ExceptionDispatchInfo[concurrency];

            try
            {
                // spawn processors, if needed
                for(int i = 0; i < concurrency; i++)
                {
                    // create process (compress/decompress) payload
                    threadsPayloads[i] = PayloadFactory.CreateProcess(queueToProcess, queueToWrite);
                    // spin it off as a separate thread
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
                {
                    // create read-process-write payload
                    var payload = PayloadFactory.CreateReadProcessWrite(InputStream, OutputStream, queueToProcess, queueToWrite);
                    // and run it on this thread
                    var payloadResult = payload.Run();
                    if(payloadResult.Status == Common.Payload.PayloadResultStatus.Failed)
                    {
                        ((Common.Payload.PayloadResultFailed)payloadResult).Failure.Throw();
                    }
                }
            }
            catch(Exception)
            {
                // abort payloads
                CancellationTokenSource.Cancel();
                // and wait they have finished
                for(var i = 0; i < concurrency; i++)
                {
                    threads[i].Join();
                }
                
                throw;
            }
            // wait processors are all finshed (should already be)
            for(int i = 0; i < concurrency; i++)
            {
                threads[i].Join();
            }
            // report exceptions if any
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