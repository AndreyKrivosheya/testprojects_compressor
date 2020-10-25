using System;
using System.IO;
using System.Threading;

using compressor.Common;
using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    class PayloadReadBlockFromBinaryFinish: Payload
    {
        public PayloadReadBlockFromBinaryFinish(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Stream stream, QueueToProcess queue)
            : base(cancellationTokenSource, settings)
        {
            this.Stream = stream;
            this.Queue = queue;
        }

        readonly Stream Stream;
        readonly QueueToProcess Queue;

        BlockToProcess LastRead = null;
        protected virtual PayloadResult RunUnsafe(IAsyncResult readingAsyncResult)
        {
            if(readingAsyncResult.IsCompleted)
            {
                try
                {
                    var totalRead = Stream.EndRead(readingAsyncResult);
                    if(totalRead != 0)
                    {
                        var data = (byte[])readingAsyncResult.AsyncState;
                        if(totalRead != data.Length)
                        {
                            if(Stream.Position < Stream.Length)
                            {
                                throw new ApplicationException("Failed to read block: block read in the middle is not of expected size");
                            }
                            else
                            {
                                LastRead = new BlockToProcess(LastRead, totalRead, data.SubArray(0, totalRead));
                                return new PayloadResultContinuationPending(LastRead);
                            }
                        }
                        else
                        {
                            LastRead = new BlockToProcess(LastRead, data.Length, data);
                            return new PayloadResultContinuationPending(LastRead);
                        }
                    }
                    else
                    {
                        // finsihed reading, no more to process
                        Queue.CompleteAdding();
                        return new PayloadResultSucceeded();
                    }
                }
                catch(Exception e)
                {
                    throw new ApplicationException("Failed to read block", e);
                }
            }

            return new PayloadResultContinuationPendingDoneNothing();
        }
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            var readingAsyncResult = parameter as IAsyncResult;
            if(readingAsyncResult == null)
            {
                throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not IAsyncResult", parameter), "parameter");
            }

            return RunUnsafe(readingAsyncResult);
        }
    }
}