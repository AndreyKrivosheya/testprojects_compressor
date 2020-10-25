using System;
using System.IO;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    class PayloadCompleteWriting : Payload
    {
        public PayloadCompleteWriting(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Stream stream)
            : base(cancellationTokenSource, settings)
        {
            this.Stream = stream;
        }

        readonly Stream Stream;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            Stream.Flush();
            return new PayloadResultSucceeded();
        }
    }
}