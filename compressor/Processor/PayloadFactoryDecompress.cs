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

        public sealed override Common.Payload.Payload CreateReadProcessWrite(Stream inputStream, Stream outputStream, QueueToProcess queueToProcess, QueueToWrite queueToWrite, IEnumerable<Thread> additionalProcessorsThreads)
        {
            // payload shared state
            // ... once reader/writer has engaged into processing (compress/decompress) you can't
            // ... "unengage" it back, due to it might still be holding block that other processors
            // ... are dependent upon adding to queue-to-write (for preserving blocks sequence order)
            var readerWriterWasEngagedIntoProcessing = false;

            // payloads
            // ... read block bytes, convert to block for queue-to-process and add to queue-to-process
            var payloadRead = FactoryBasic.Chain(
                //  read block bytes and convert to block for queue-to-process
                FactoryBasic.Chain(
                    // read block bytes
                    FactoryBasic.Chain(
                        // read block length
                        FactoryBasic.Chain(
                            // read block length bytes
                            FactoryBasic.Chain(
                                FactoryBasic.ReturnConstant(sizeof(long)),
                                FactoryCommonStreams.ReadBytesExactly(inputStream,
                                    exceptionProducer: (e) => new ApplicationException("Failed to read block length"),
                                    onReadPastStreamEnd: () => { queueToProcess.CompleteAdding(); }
                                )
                            ),
                            // convert block length bytes to long
                            FactoryCommonConvert.BytesToLong()
                        ),
                        // read block
                        FactoryCommonStreams.ReadBytesExactly(inputStream,
                            exceptionProducer: (e) => new ApplicationException("Failed to read block"),
                            onReadPastStreamEnd: () => { throw new ApplicationException("Unexpected end of stream: read block length, but not the block"); }
                        )
                    ),
                    // convert bytes read to block for queue-to-process
                    FactoryProcessor.BytesToBlockToProcessArchive()
                ),
                // add block read to queue-to-process
                FactoryProcessor.QueueAddToQueueToProcess(queueToProcess, 0)
            );
            // ... if engaged: get block out of queue-process, process, and add to queue-to-write
            // ... if processing completed, finilize queue-to-write
            var payloadProcess = ProcessSequence(Ref.Of(() => readerWriterWasEngagedIntoProcessing), queueToProcess, queueToWrite, additionalProcessorsThreads);
            // ... if writing completed, finilize
            // ... get blocks from queue-to-process, convert to bytes and write to archive
            // ... get blocks from queue-to-process, convert to bytes and write to archive
            var payloadWrite = FactoryBasic.Sequence(
                CompleteWritingConditional(outputStream, queueToWrite),
                FactoryBasic.Chain(
                    // get blocks from queue-to-write
                    FactoryProcessor.QueueGetOneOrMoreFromQueueToWrite(queueToWrite, 0),
                    // convert blocks to bytes
                    FactoryProcessor.BlocksToWriteToBytesBinary(),
                    // write bytes
                    FactoryCommonStreams.WriteBytes(outputStream,
                        exceptionProducer: (e) => new ApplicationException("Failed to write block", e))
                )
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