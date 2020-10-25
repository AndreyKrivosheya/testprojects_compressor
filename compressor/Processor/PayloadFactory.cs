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

        protected readonly CancellationTokenSource CancellationTokenSource;
        protected readonly SettingsProvider Settings;

        readonly Lazy<Common.Payload.Basic.Factory> FactoryBasicLazy;
        protected Common.Payload.Basic.Factory FactoryBasic { get { return FactoryBasicLazy.Value; } }

        readonly Lazy<Common.Payload.Streams.Factory> FactoryCommonStreamsLazy;
        protected Common.Payload.Streams.Factory FactoryCommonStreams { get { return FactoryCommonStreamsLazy.Value; } }

        readonly Lazy<Common.Payload.Convert.Factory> FactoryCommonConvertLazy;
        protected Common.Payload.Convert.Factory FactoryCommonConvert { get { return FactoryCommonConvertLazy.Value; } }

        readonly Lazy<Factory> FactoryProcessorLazy;
        protected Factory FactoryProcessor { get { return FactoryProcessorLazy.Value; } }
        
        protected abstract Common.Payload.Payload CreateProcessPayload();

        public Common.Payload.Payload CreateProcessBody(QueueToProcess queueToProcess, QueueToWrite queueToWrite, int queueOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain(
                FactoryProcessor.QueueGetOneFromQueueToProcess(queueToProcess, queueOperationTimeoutMilliseconds),
                CreateProcessPayload(),
                FactoryProcessor.QueueAddToQueueToWrite(queueToWrite, queueOperationTimeoutMilliseconds)
            );
        }

        public Common.Payload.Payload CreateProcess(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            return FactoryBasic.Repeat(
                CreateProcessBody(queueToProcess, queueToWrite, Timeout.Infinite)
            );
        }

        protected Common.Payload.Payload ProcessSequence(Ref<bool> readerWriterWasEngagedIntoProcessing, QueueToProcess queueToProcess, QueueToWrite queueToWrite, IEnumerable<Thread> additionalProcessorsThreads)
        {
            return FactoryBasic.Sequence(
                FactoryBasic.Conditional(
                    () => readerWriterWasEngagedIntoProcessing.Value = readerWriterWasEngagedIntoProcessing.Value || queueToProcess.IsCompleted || (queueToProcess.IsHalfFull() || !additionalProcessorsThreads.Any(x => x != null && x.IsAlive)),
                    CreateProcessBody(queueToProcess, queueToWrite, 0)
                ),
                FactoryBasic.Conditional(
                    () => queueToProcess.IsCompleted && !additionalProcessorsThreads.Any(x => x != null && x.IsAlive),
                    FactoryProcessor.CompleteProcessing(queueToWrite)
                )
            );
        }

        protected Common.Payload.Payload CompleteWritingConditional(Stream outputStream, QueueToWrite queueToWrite)
        {
            return FactoryBasic.Conditional(
                () => queueToWrite.IsCompleted,
                FactoryProcessor.CompleteWriting(outputStream)
            );
        }

        public abstract Common.Payload.Payload CreateReadProcessWrite(Stream inputStream, Stream outputStream, QueueToProcess queueToProcess, QueueToWrite queueToWrite, IEnumerable<Thread> additionalProcessorsThreads);
    }
}