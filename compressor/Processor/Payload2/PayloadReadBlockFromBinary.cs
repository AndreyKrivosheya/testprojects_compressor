using System;
using System.IO;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    class PayloadReadBlockFromBinary: Payload
    {
        public PayloadReadBlockFromBinary(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Stream stream, QueueToProcess queue)
            : base(cancellationTokenSource, settings)
        {
            this.Stream = stream;
            this.Queue = queue;
        }

        readonly Stream Stream;
        readonly QueueToProcess Queue;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            var data = new byte[Settings.BlockSize];
            try
            {
                try
                {
                    var readingAsyncResult = Stream.BeginRead(data, 0, data.Length, null, data);
                    return new PayloadResultContinuationPending(readingAsyncResult);
                }
                catch(IOException)
                {
                    // reading past stream end, means we have read all the stream
                    Queue.CompleteAdding();
                    return new PayloadResultSucceeded();
                }
            }
            catch(Exception e)
            {
                throw new ArgumentNullException("Failed to read block", e);
            }
        }
    }
}