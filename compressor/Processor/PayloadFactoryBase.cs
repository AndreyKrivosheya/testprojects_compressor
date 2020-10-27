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
    abstract class PayloadFactoryBase : PayloadFactory
    {
        const int TimeoutImmediate = 0;

        public PayloadFactoryBase(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings)
        {
        }
        public PayloadFactoryBase(SettingsProvider settings)
            : base(settings)
        {
        }

        #region Processor payload factory

        // creates immediate compress/decompress processor paylod
        protected abstract Common.Payload.Payload CreateProcessPayload();

        // creates payload tree implementing single run of read-process-write payload
        protected Common.Payload.Payload CreateProcessSubpayload(QueueToProcess queueToProcess, QueueToWrite queueToWrite, int queueOperationTimeoutMilliseconds)
        {
            return FactoryBasic.WhenSucceeded(
                FactoryBasic.Chain(
                    FactoryProcessor.QueueGetOneFromQueueToProcess(queueToProcess, queueOperationTimeoutMilliseconds),
                    CreateProcessPayload(),
                    FactoryProcessor.QueueAddToQueueToWrite(queueToWrite, queueOperationTimeoutMilliseconds)
                ),
                FactoryBasic.Conditional(
                    (BlockToWrite lastBlockAddedByThisPayload) => lastBlockAddedByThisPayload.Last,
                    FactoryProcessor.QueueCompleteAddingQueueToWrite(queueToWrite),
                    FactoryBasic.Succeed()
                )
            );
        }

        public sealed override Common.Payload.Payload CreateProcess(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            return FactoryBasic.Repeat(
                CreateProcessSubpayload(queueToProcess, queueToWrite, Timeout.Infinite)
            );
        }

        #endregion

        #region ReaderProcessorWrtier payload factory

        #region ReaderProcessorWriter Reader subpayload factory

        // creates immediate block read bytes from input payload
        protected abstract Common.Payload.Payload CreateReadBlockBytesPayload(Stream inputStream, int streamOperationTimeoutMilliseconds);

        // creates immediate bytes read to block for queue-to-process convertion payload
        protected abstract Common.Payload.Payload CreateBytesToBlockToProcessPayload();

        // creates read subpayload of payload tree implementing run of compress/decompress processor payload
        Common.Payload.Payload CreateReadProcessWriteSubpayloadRead(Stream inputStream, QueueToProcess queueToProcess)
        {
            // read block bytes, convert to block for queue-to-process and add to queue-to-process
            // when reading completed close queue-to-process for additions
            return FactoryBasic.WhenSucceeded(
                FactoryBasic.Chain(
                    //  read block bytes and convert to block for queue-to-process
                    FactoryBasic.Chain(
                        CreateReadBlockBytesPayload(inputStream, TimeoutImmediate),
                        CreateBytesToBlockToProcessPayload()
                    ),
                    // add block read to queue-to-process
                    FactoryProcessor.QueueAddToQueueToProcess(queueToProcess, TimeoutImmediate)
                ),
                // complete adding
                FactoryProcessor.QueueCompleteAddingQueueToProcess(queueToProcess)
            );
        }
       
        #endregion

        #region ReaderProcessorWriter Processor subpayload factory

        // creates process subpayload of payload tree implementing run of compress/decompress processor payload
        Common.Payload.Payload CreateReadProcessWriteSubpayloadProcess(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            // if engaged: get block out of queue-to-process, process (compress/decompress) and add result to queue-to-write
            // when processing completed close queue-to-write for additions
            return FactoryBasic.ConditionalOnceAndForever(
                () => queueToProcess.IsFull(),
                CreateProcessSubpayload(queueToProcess, queueToWrite, TimeoutImmediate)
            );
        }

        #endregion

        #region ReaderProcessorWriter Writer subpayload factory

        // creates immediate blocks for queue-to-write to bytes for writing convertion payload
        protected abstract Common.Payload.Payload CreateBlocksToWriteToBytesPayload();

        // creates write subpayload tree of payload tree implementing run of compress/decompress processor payload
        Common.Payload.Payload CreateReadProcessWriteSubpayloadWrite(QueueToWrite queueToWrite, Stream outputStream)
        {
            // get blocks from queue-to-write, convert to bytes and write to archive
            // when writing completed, finalize
            return FactoryBasic.WhenSucceeded(
                FactoryBasic.Chain(
                    // get blocks from queue-to-write
                    FactoryProcessor.QueueGetOneOrMoreFromQueueToWrite(queueToWrite, TimeoutImmediate, Settings.MaxBlocksToWriteAtOnce),
                    // convert blocks to bytes
                    CreateBlocksToWriteToBytesPayload(),
                    // write bytes
                    FactoryCommonStreams.WriteBytes(outputStream, TimeoutImmediate,
                        exceptionProducer: (e) => new ApplicationException("Failed to write block", e))
                ),
                FactoryBasic.Chain(
                    FactoryCommonStreams.Flush(outputStream),
                    FactoryBasic.Succeed()
                )
            );
        }

        #endregion

        // creates payload tree implementing run of compress/decompress processor payload
        public sealed override Common.Payload.Payload CreateReadProcessWrite(Stream inputStream, Stream outputStream, QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            return FactoryBasic.Repeat(FactoryBasic.Sequence(
                (CreateReadProcessWriteSubpayloadRead(inputStream, queueToProcess), true),
                (CreateReadProcessWriteSubpayloadWrite(queueToWrite, outputStream), true),
                (CreateReadProcessWriteSubpayloadProcess(queueToProcess, queueToWrite), false)
            ));
        }

        #endregion
    }
}