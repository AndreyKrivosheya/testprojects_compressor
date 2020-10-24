using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Processor.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class PayloadFactory
    {
        public PayloadFactory(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
        {
            this.CancellationTokenSource = cancellationTokenSource;
            this.Settings = settings;
            this.FactoryBasicLazy = new Lazy<Common.Payload.Basic.Factory>(() => new Common.Payload.Basic.Factory(CancellationTokenSource));
            this.FactoryCommonStreamsLazy = new Lazy<Common.Payload.Streams.Factory>(() => new Common.Payload.Streams.Factory(CancellationTokenSource));
            this.FactoryCommonConvertLazy = new Lazy<Common.Payload.Convert.Factory>(() => new Common.Payload.Convert.Factory(CancellationTokenSource));
            this.FactoryProcessorLazy = new Lazy<Factory>(() => new Factory(CancellationTokenSource, Settings));
        }
        public PayloadFactory(SettingsProvider settings)
            : this(new CancellationTokenSource(), settings)
        {
        }

        public readonly CancellationTokenSource CancellationTokenSource;
        protected readonly SettingsProvider Settings;

        readonly Lazy<Common.Payload.Basic.Factory> FactoryBasicLazy;
        protected Common.Payload.Basic.Factory FactoryBasic { get { return FactoryBasicLazy.Value; } }

        readonly Lazy<Common.Payload.Streams.Factory> FactoryCommonStreamsLazy;
        protected Common.Payload.Streams.Factory FactoryCommonStreams { get { return FactoryCommonStreamsLazy.Value; } }

        readonly Lazy<Common.Payload.Convert.Factory> FactoryCommonConvertLazy;
        protected Common.Payload.Convert.Factory FactoryCommonConvert { get { return FactoryCommonConvertLazy.Value; } }

        readonly Lazy<Factory> FactoryProcessorLazy;
        protected Factory FactoryProcessor { get { return FactoryProcessorLazy.Value; } }
        
        // creates immediate compress/decompress processor paylod
        protected abstract Common.Payload.Payload CreateProcessPayload();

        // creates payload tree implementing single run of compress/decompress processor payload
        public Common.Payload.Payload CreateProcessBody(QueueToProcess queueToProcess, QueueToWrite queueToWrite, int queueOperationTimeoutMilliseconds)
        {
            return FactoryBasic.WhenFinished(
                FactoryBasic.Chain(
                    FactoryProcessor.QueueGetOneFromQueueToProcess(queueToProcess, queueOperationTimeoutMilliseconds),
                    CreateProcessPayload(),
                    FactoryProcessor.QueueAddToQueueToWrite(queueToWrite, queueOperationTimeoutMilliseconds)
                ),
                FactoryBasic.Conditional(
                    (parameter) => object.ReferenceEquals(parameter, PayloadQueueCompleteAdding.LastObjectAdded),
                    FactoryProcessor.QueueCompleteAddingQueueToWrite(queueToWrite, queueOperationTimeoutMilliseconds),
                    FactoryBasic.Succeed()
                )
            );
        }

        public Common.Payload.Payload CreateProcess(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            return FactoryBasic.Repeat(
                CreateProcessBody(queueToProcess, queueToWrite, Timeout.Infinite)
            );
        }

        public abstract Common.Payload.Payload CreateReadProcessWrite(Stream inputStream, Stream outputStream, QueueToProcess queueToProcess, QueueToWrite queueToWrite);
    }
}