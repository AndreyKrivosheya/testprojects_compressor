using System;
using System.IO;
using System.Threading;

using compressor.Processor.Settings;

namespace compressor.Common.Payload.Streams
{
    class PayloadWriteBytesStart: Payload
    {
        public PayloadWriteBytesStart(CancellationTokenSource cancellationTokenSource, Stream stream, Func<Exception, Exception> exceptionProducer)
            : base(cancellationTokenSource, stream)
        {
            this.ExceptionProducer = exceptionProducer;
        }

        readonly Func<Exception, Exception> ExceptionProducer;

        PayloadResult RunUnsafe(byte[] bytes)
        {
            if(bytes.Length > 0)
            {
                try
                {
                    var writingAsyncResult = Stream.BeginWrite(bytes, 0, bytes.Length, null, null);
                    return new PayloadResultContinuationPending(writingAsyncResult);
                }
                catch(Exception e)
                {
                    throw ExceptionProducer(e);
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

            var bytes = parameter as byte[];
            if(bytes == null)
            {
                throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not byte[]", parameter), "parameter");
            }

            return RunUnsafe(bytes);
        }
    }
}