using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    abstract class PayloadFactory
    {
        public PayloadFactory(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
        {
            this.CancellationTokenSource = cancellationTokenSource;
            this.Settings = settings;
            this.FactoryBasicLazy = new Lazy<Common.Payload.Basic.Factory>(() => new Common.Payload.Basic.Factory(CancellationTokenSource));
            this.FactoryCommonStreamsLazy = new Lazy<Common.Payload.Streams.Factory>(() => new Common.Payload.Streams.Factory(CancellationTokenSource));
            this.FactoryProcessorLazy = new Lazy<Factory>(() => new Factory(CancellationTokenSource, Settings));
        }

        protected readonly CancellationTokenSource CancellationTokenSource;
        protected readonly SettingsProvider Settings;

        readonly Lazy<Common.Payload.Basic.Factory> FactoryBasicLazy;
        protected Common.Payload.Basic.Factory FactoryBasic { get { return FactoryBasicLazy.Value; } }

        readonly Lazy<Common.Payload.Streams.Factory> FactoryCommonStreamsLazy;
        protected Common.Payload.Streams.Factory FactoryCommonStreams { get { return FactoryCommonStreamsLazy.Value; } }

        readonly Lazy<Factory> FactoryProcessorLazy;
        protected Factory FactoryProcessor { get { return FactoryProcessorLazy.Value; } }
        
        public abstract Common.Payload.Payload CreateProcess(QueueToProcess queueToProcess, QueueToWrite queueToWrite);
        public abstract Common.Payload.Payload CreateReadProcessWrite(Stream inputStream, Stream outputStream, QueueToProcess queueToProcess, QueueToWrite queueToWrite, IEnumerable<Thread> additionalProcessorsThreads);
    }
}