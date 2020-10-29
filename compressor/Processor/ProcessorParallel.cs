using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Common;
using compressor.Common.Threading;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class ProcessorParallel: Processor
    {
        public ProcessorParallel(SettingsProvider settings, Stream inputStream, Stream outputStream, Func<CancellationTokenSource, SettingsProvider, PayloadFactory> payloadFactoryCreator)
            : base(settings, inputStream, outputStream)
        {
            this.PayloadFactoryCreator = payloadFactoryCreator;
        }

        protected readonly Func<CancellationTokenSource, SettingsProvider, PayloadFactory> PayloadFactoryCreator;
        
        protected sealed override void RunOnThread()
        {
            var queueSize = Settings.MaxQueueSize;
            using(var queueToProcess = new QueueToProcess(queueSize))
            {
                using(var queueToWrite = new QueueToWrite(queueSize))
                {
                    using(var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var payloadFactory = PayloadFactoryCreator(cancellationTokenSource, Settings);
                        var concurrency = Settings.MaxConcurrency - 1;
                        var threads = new IAsyncResult[concurrency];
                        var threadsPayloads = new Common.Payload.Payload[concurrency];
                        var threadsErrors = new ExceptionDispatchInfo[concurrency];

                        try
                        {
                            // spawn processors, if needed
                            for(int i = 0; i < concurrency; i++)
                            {
                                // create process (compress/decompress) payload
                                threadsPayloads[i] = payloadFactory.CreateProcess(queueToProcess, queueToWrite);
                                // spin it off as a separate thread
                                threads[i] = Threads.QueueAndRun((object idxRaw) => {
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
                                }, state: i, name: string.Format("Processor[{0}]: Process worker", i));
                            }
                            // run reader/processor/writer
                            {
                                // create read-process-write payload
                                var payload = payloadFactory.CreateReadProcessWrite(InputStream, OutputStream, queueToProcess, queueToWrite);
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
                            cancellationTokenSource.Cancel();
                            // and wait they have finished
                            for(var i = 0; i < concurrency; i++)
                            {
                                threads[i].WaitCompleted();
                            }
                            
                            throw;
                        }
                        // wait processors are all finshed (should already be)
                        for(int i = 0; i < concurrency; i++)
                        {
                            threads[i].WaitCompleted();
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
        }
    }
}