using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Payload;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    sealed class PayloadFactoryCompress: PayloadFactoryBase
    {
        public static readonly Func<CancellationTokenSource, SettingsProvider, PayloadFactory> Creator =
            (cancellationTokenSource, settings) => new PayloadFactoryCompress(cancellationTokenSource, settings);

        public PayloadFactoryCompress(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings)
        {
        }
        public PayloadFactoryCompress(SettingsProvider settings)
            : base(settings)
        {
        }

        #region Processor payload factory

        // creates immediate compress/decompress processor paylod
        protected sealed override Common.Payload.Payload CreateProcessPayload()
        {
            return FactoryProcessor.ProcessCompress();
        }

        #endregion

        #region ReaderProcessorWrtier payload factory

        #region ReaderProcessorWriter Reader subpayload factory

        // creates immediate block read bytes from input payload
        protected sealed override Common.Payload.Payload CreateReadBlockBytesPayload(Stream inputStream, int streamOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain(
                FactoryBasic.ReturnValue(Settings.BlockSize),
                FactoryCommonStreams.ReadBytesNoMoreThen(inputStream, streamOperationTimeoutMilliseconds,
                    exceptionProducer: (e) => new ArgumentNullException("Failed to read block", e))
            );
        }

        // creates immediate bytes read to block for queue-to-process convertion payload
        protected sealed override Common.Payload.Payload CreateBytesToBlockToProcessPayload()
        {
            return FactoryProcessor.BytesToBlockToProcessBinary();
        }

        #endregion

        #region ReaderProcessorWriter Writer subpayload factory

        // creates immediate blocks for queue-to-write to bytes for writing convertion payload
        protected sealed override Common.Payload.Payload CreateBlocksToWriteToBytesPayload()
        {
            return FactoryProcessor.BlocksToWriteToBytesArchive();
        }

        #endregion

        #endregion
    }
}