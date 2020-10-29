using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class ProcessorNotParallel: ProcessorParallel
    {
        class SettingsProviderOverrideConcurrencyToOne: SettingsProvider
        {
            public SettingsProviderOverrideConcurrencyToOne(SettingsProvider baseSettings)
            {
                this.BaseSettings = baseSettings;
            }

            readonly SettingsProvider BaseSettings;
            
            public int MaxConcurrency { get { return 1; } }

            public int MaxQueueSize { get { return BaseSettings.MaxQueueSize; } }

            public long BlockSize { get { return BaseSettings.BlockSize; } }

            public int MaxBlocksToWriteAtOnce { get { return BaseSettings.MaxBlocksToWriteAtOnce; } }
        } 

        public ProcessorNotParallel(SettingsProvider settings, Stream inputStream, Stream outputStream, Func<CancellationTokenSource, SettingsProvider, PayloadFactory> payloadFactory)
            : base(new SettingsProviderOverrideConcurrencyToOne(settings), inputStream, outputStream, payloadFactory)
        {
        }
    }
}