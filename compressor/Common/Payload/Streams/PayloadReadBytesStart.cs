using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadReadBytesStart: Payload
    {
        public PayloadReadBytesStart(CancellationTokenSource cancellationTokenSource, Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
            : base(cancellationTokenSource, stream)
        {
            this.OnReadPastStreamEnd = onReadPastStreamEnd;
            this.ExceptionProducer = exceptionProducer;
        }

        readonly Action OnReadPastStreamEnd;
        readonly Func<Exception, Exception> ExceptionProducer;

        PayloadResult RunUnsafe(int length)
        {
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
                throw ExceptionProducer(e);
            }
        }
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            try
            {
                var length = System.Convert.ToInt32(parameter);
                if(length < 1)
                {
                    throw new ArgumentException(string.Format("Value of 'parameter' ({0}) could not be less then 1", parameter), "parameter");
                }
                return RunUnsafe(length);
            }
            catch(Exception e)
            {
                if(e is InvalidCastException || e is FormatException || e is OverflowException)
                {
                    throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not int", parameter), "parameter", e);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}