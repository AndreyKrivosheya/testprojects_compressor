using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    sealed class PayloadFactoryCompress: PayloadFactory
    {
        public PayloadFactoryCompress(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings)
        {
        }

        Common.Payload.Payload CreateProcessChain(QueueToProcess queueToProcess, QueueToWrite queueToWrite, int queueOperationTimeoutMilliseconds)
        {
            return FactoryBasic.CreateChain(
                FactoryProcessor.CreateQueueGetOneFromQueueToProcess(queueToProcess, queueOperationTimeoutMilliseconds),
                FactoryProcessor.CreateProcessCompress(),
                FactoryProcessor.CreateQueueAddToQueueToWrite(queueToWrite, queueOperationTimeoutMilliseconds)
            );
        }

        public sealed override Common.Payload.Payload CreateProcess(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            return FactoryBasic.CreateRepeat(
                CreateProcessChain(queueToProcess, queueToWrite, Timeout.Infinite)
            );
        }

        public sealed override Common.Payload.Payload CreateReadProcessWrite(Stream inputStream, Stream outputStream, QueueToProcess queueToProcess, QueueToWrite queueToWrite, IEnumerable<Thread> additionalProcessorsThreads)
        {
            // payload shared state
            // ... once reader/writer has engaged into processing (compress/decompress) you can't
            // ... "unengage" it back, due to it might still be holding block that other processors
            // ... are dependent upon adding to queue-to-write (for preserving blocks sequence order)
            var readerWriterWasEngagedIntoProcessing = false;

            return FactoryBasic.CreateRepeat(
                FactoryBasic.CreateSequence(
                    // read
                    FactoryBasic.CreateChain(
                        //  read
                        FactoryBasic.CreateChain(
                            FactoryProcessor.CreateReadBlockFromBinary(inputStream, queueToProcess),
                            FactoryProcessor.CreateReadBlockFromBinaryFinish(inputStream, queueToProcess)
                        ),
                        // add block read to queue-to-process
                        FactoryProcessor.CreateQueueAddToQueueToProcess(queueToProcess, 0)
                    ),
                    // process
                    FactoryBasic.CreateSequence(
                        FactoryBasic.CreateConditional(
                            () => queueToProcess.IsCompleted && !additionalProcessorsThreads.Any(x => x == null || x.IsAlive),
                            FactoryProcessor.CreateCompleteProcessing(queueToWrite)
                        ),
                        FactoryBasic.CreateConditional(
                            () => readerWriterWasEngagedIntoProcessing = readerWriterWasEngagedIntoProcessing || queueToProcess.IsCompleted || (queueToProcess.IsHalfFull() || !additionalProcessorsThreads.Any(x => x != null && x.IsAlive)),
                            CreateProcessChain(queueToProcess, queueToWrite, 0)
                        )
                    ),
                    // write
                    FactoryBasic.CreateSequence(
                        FactoryBasic.CreateConditional(
                            () => queueToWrite.IsCompleted,
                            FactoryProcessor.CreateCompleteWriting(outputStream)
                        ),
                        FactoryBasic.CreateChain(
                            FactoryProcessor.CreateGetOneOrMoreFromQueueToWrite(queueToWrite, 0),
                            FactoryProcessor.CreateBlocksToWriteToBytesArchive(),
                            FactoryCommonStreams.CreateWriteBytesChain(outputStream)
                        )
                    )
                )
            );
        }
    }
}