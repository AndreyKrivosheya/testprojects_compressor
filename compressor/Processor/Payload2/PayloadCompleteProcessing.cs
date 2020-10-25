using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    class PayloadCompleteProcessing : Payload
    {
        public PayloadCompleteProcessing(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, QueueToWrite queue)
            : base(cancellationTokenSource, settings)
        {
            this.Queue = queue;
        }

        readonly QueueToWrite Queue;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            Queue.CompleteAdding();
            return new PayloadResultSucceeded();
        }
    }
}