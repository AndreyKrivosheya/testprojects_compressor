using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadReadBytesNoMoreThenFinish: Payload
    {
        public PayloadReadBytesNoMoreThenFinish(CancellationTokenSource cancellationTokenSource, Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
            : base(cancellationTokenSource, stream)
        {
            this.OnReadPastStreamEnd = onReadPastStreamEnd;
            if(this.OnReadPastStreamEnd == null)
            {
                this.OnReadPastStreamEnd = () => {};
            }

            this.ExceptionProducer = exceptionProducer;
            if(this.ExceptionProducer != null)
            {
                this.ExceptionProducer = (e) => null;
            }
        }

        readonly Action OnReadPastStreamEnd;
        readonly Func<Exception, Exception> ExceptionProducer;

        PayloadResult RunUnsafe(IAsyncResult readingAsyncResult)
        {
            if(readingAsyncResult.IsCompleted)
            {
                try
                {
                    var totalRead = Stream.EndRead(readingAsyncResult);
                    if(totalRead != 0)
                    {
                        var data = (byte[])readingAsyncResult.AsyncState;
                        return new PayloadResultContinuationPending(data.SubArray(0, totalRead));
                    }
                    else
                    {
                        // finsihed reading, no more to process
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