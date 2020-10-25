using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadReadBytesStart: Payload
    {
        public PayloadReadBytesStart(CancellationTokenSource cancellationTokenSource, Stream stream, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
            : base(cancellationTokenSource, stream)
        {
            this.OnReadPastStreamEnd = onReadPastStreamEnd;
            if(this.OnReadPastStreamEnd == null)
            {
                this.OnReadPastStreamEnd = () => {};
            }

            this.ExceptionProducer = exceptionProducer;
            if(this.ExceptionProducer == null)
            {
                this.ExceptionProducer = (e) => null; 
            }
        }

        readonly Action OnReadPastStreamEnd;
        readonly Func<Exception, Exception> ExceptionProducer;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((int length) =>
            {
                if(length < 1)
                {
                    throw new ArgumentException(string.Format("Value of 'parameter' ({0}) could not be less then 1", parameter), "parameter");
                }

                var data = new byte[length];
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
                        OnReadPastStreamEnd();
                        return new PayloadResultSucceeded();
                    }
                }
                catch(Exception e)
                {
                    var eNew = ExceptionProducer(e);
                    if(eNew != null)
                    {
                        throw eNew;
                    }
                    else
                    {
                        throw;
                    }
                }
            });
        }
    }
}