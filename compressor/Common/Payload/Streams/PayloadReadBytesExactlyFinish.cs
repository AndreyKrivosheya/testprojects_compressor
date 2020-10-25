using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadReadBytesExactlyFinish: Payload
    {
        public PayloadReadBytesExactlyFinish(CancellationTokenSource cancellationTokenSource, Stream stream, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
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

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return VerifyParameterNotNullConvertAndRunUnsafe(parameter,
            (IAsyncResult readingAsyncResult) =>
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
                                throw new ApplicationException(string.Format("Read ({0}) less then expected ({1})", totalRead, data.Length));
                            }
                            else
                            {
                                return new PayloadResultContinuationPending(data);
                            }
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
            });
        }
    }
}