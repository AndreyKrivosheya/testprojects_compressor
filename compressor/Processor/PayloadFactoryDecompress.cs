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
    sealed class PayloadFactoryDecompress: PayloadFactory
    {
        public PayloadFactoryDecompress(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings)
        {
        }
        public PayloadFactoryDecompress(SettingsProvider settings)
            : base(settings)
        {
        }

        protected sealed override Common.Payload.Payload CreateProcessPayload()
        {
            return FactoryProcessor.ProcessDecompress();
        }

        public sealed override Common.Payload.Payload CreateReadProcessWrite(Stream inputStream, Stream outputStream, QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            // payloads
            // ... read block bytes
            var payloadReadBlockBytes = FactoryBasic.Chain(
                // read block length
                FactoryBasic.Chain(
                    // read block length bytes
                    FactoryBasic.Chain(
                        FactoryBasic.ReturnConstant(sizeof(long)),
                        FactoryCommonStreams.ReadBytesExactly(inputStream,
                            exceptionProducer: (e) => new ApplicationException("Failed to read block length"))
                    ),
                    // convert block length bytes to long
                    FactoryCommonConvert.BytesToLong()
                ),
                // read block
                FactoryCommonStreams.ReadBytesExactly(inputStream,
                    exceptionProducer: (e) => new ApplicationException("Failed to read block"),
                    onReadPastStreamEnd: () => { throw new ApplicationException("Unexpected end of stream: read block length, but not the block"); }
                )
            );
            // ... read block bytes, convert to block for queue-to-process and add to queue-to-process
            // ... when reading completed close queue-to-process for additions
            var payloadRead = FactoryBasic.WhenFinished(
                FactoryBasic.Chain(
                    //  read block bytes and convert to block for queue-to-process
                    FactoryBasic.Chain(
                        payloadReadBlockBytes,
                        FactoryProcessor.BytesToBlockToProcessArchive()
                    ),
                    // add block read to queue-to-process
                    FactoryProcessor.QueueAddToQueueToProcess(queueToProcess, 0)
                ),
                FactoryProcessor.QueueCompleteAddingQueueToProcess(queueToProcess, 0)
            );
            // ... if engaged: get block out of queue-to-process, process (compress/decompress),
            // ... and add result to queue-to-write
            // ... when processing completed close queue-to-write for additions
            var payloadProcess = FactoryBasic.ConditionalOnceAndForever(
                () => queueToWrite.IsCompleted || queueToProcess.IsHalfFull(),
                CreateProcessBody(queueToProcess, queueToWrite, 0)
            );
            // ... get blocks from queue-to-process, convert to bytes and write to archive
            // ... when writing completed, finilize
            var payloadWrite = FactoryBasic.WhenFinished(
                FactoryBasic.Chain(
                    // get blocks from queue-to-write
                    FactoryProcessor.QueueGetOneOrMoreFromQueueToWrite(queueToWrite, 0),
                    // convert blocks to bytes
                    FactoryProcessor.BlocksToWriteToBytesBinary(),
                    // write bytes
                    FactoryCommonStreams.WriteBytes(outputStream,
                        exceptionProducer: (e) => new ApplicationException("Failed to write block", e))
                ),
                FactoryProcessor.CompleteWriting(outputStream)
            );

            return FactoryBasic.Repeat(
                FactoryBasic.Sequence(
                    payloadRead,
                    payloadProcess,
                    payloadWrite
                )
            );
        }
    }
}