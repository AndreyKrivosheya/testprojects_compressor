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
    sealed class PayloadFactoryDecompress: PayloadFactoryBase
    {
        public static readonly Func<CancellationTokenSource, SettingsProvider, PayloadFactory> Creator =
            (cancellationTokenSource, settings) => new PayloadFactoryDecompress(cancellationTokenSource, settings);

        public PayloadFactoryDecompress(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings)
        {
        }

        #region Processor payload factory

        // creates immediate compress/decompress processor paylod
        protected sealed override Common.Payload.Payload CreateProcessPayload()
        {
            return FactoryProcessor.ProcessDecompress();
        }


        #endregion

        #region ReaderProcessorWrtier payload factory

        #region ReaderProcessorWriter Reader subpayload factory

        // creates immediate block read bytes from input payload
        protected sealed override Common.Payload.Payload CreateReadBlockBytesPayload(Stream inputStream, int streamOperationTimeoutMilliseconds)
        {
            return FactoryBasic.Chain(
                // read block length
                // ... read block length bytes
                FactoryBasic.ReturnValue(sizeof(long)),
                FactoryCommonStreams.ReadBytesExactly(inputStream, streamOperationTimeoutMilliseconds,
                    exceptionProducer: (e) => new ApplicationException("Failed to read block length")),
                // ... convert block length bytes to long
                FactoryCommonConvert.BytesToLong(),
                // read block
                FactoryCommonStreams.ReadBytesExactly(inputStream, streamOperationTimeoutMilliseconds,
                    exceptionProducer: (e) => new ApplicationException("Failed to read block"),
                    onReadPastStreamEnd: () => { throw new ApplicationException("Unexpected end of stream: read block length, but not the block"); }
                )
            );
        }

        // creates immediate bytes read to block for queue-to-process convertion payload
        protected sealed override Common.Payload.Payload CreateBytesToBlockToProcessPayload()
        {
            return FactoryProcessor.BytesToBlockToProcessArchive();
        }

        #endregion

        #region ReaderProcessorWriter Writer subpayload factory

        // creates immediate blocks for queue-to-write to bytes for writing convertion payload
        protected sealed override Common.Payload.Payload CreateBlocksToWriteToBytesPayload()
        {
            return FactoryProcessor.BlocksToWriteToBytesBinary();
        }

        #endregion

        #endregion
    }
}