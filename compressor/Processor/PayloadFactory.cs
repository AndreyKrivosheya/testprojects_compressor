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
            
            this.FactoryBasic = new Common.Payload.Basic.Factory(CancellationTokenSource);
            this.FactoryCommonStreams = new Common.Payload.Streams.Factory(CancellationTokenSource);
            this.FactoryCommonConvert = new Common.Payload.Convert.Factory(CancellationTokenSource);
            this.FactoryProcessor = new Factory(CancellationTokenSource);
        }

        protected readonly CancellationTokenSource CancellationTokenSource;
        protected readonly SettingsProvider Settings;

        protected readonly Common.Payload.Basic.Factory FactoryBasic;
        protected readonly Common.Payload.Streams.Factory FactoryCommonStreams;
        protected readonly Common.Payload.Convert.Factory FactoryCommonConvert;
        protected readonly Factory FactoryProcessor;
        
        // creates payload tree implementing run of compress/decompress payload
        public abstract Common.Payload.Payload CreateProcess(QueueToProcess queueToProcess, QueueToWrite queueToWrite);

        // creates payload tree implementing run of read-compress/decompress-write payload
        public abstract Common.Payload.Payload CreateReadProcessWrite(Stream inputStream, Stream outputStream, QueueToProcess queueToProcess, QueueToWrite queueToWrite);
    }
}