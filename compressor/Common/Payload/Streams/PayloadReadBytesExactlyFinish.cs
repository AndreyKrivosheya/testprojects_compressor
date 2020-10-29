using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadReadBytesExactlyFinish: PayloadReadBytesFinish
    {
        public PayloadReadBytesExactlyFinish(CancellationTokenSource cancellationTokenSource, Stream stream, int streamOperationTimeoutMilleseconds, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
            : base(cancellationTokenSource, stream, streamOperationTimeoutMilleseconds)
        {
            this.OnReadPastStreamEnd = onReadPastStreamEnd;
            if(this.OnReadPastStreamEnd == null)
            {
                this.OnReadPastStreamEnd = () => {};
            }
            
            this.ExceptionProducer = exceptionProducer;
            if(this.ExceptionProducer == null)
            {
                this.ExceptionProducer = (e) => e;
            }
        }


        readonly Action OnReadPastStreamEnd;
        readonly Func<Exception, Exception> ExceptionProducer;

        protected sealed override PayloadResult RunUnsafe(IAsyncResult completedReadingAsyncResult)
        {
            try
            {
                var totalRead = Stream.EndRead(completedReadingAsyncResult);
                if(totalRead != 0)
                {
                    var data = (byte[])completedReadingAsyncResult.AsyncState;
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
                    if(!object.ReferenceEquals(e, eNew))
                    {
                        throw eNew;
                    }
                    else
                    {
                        throw;
                    }
                }
                else
                {
                    return new PayloadResultContinuationPendingDoneNothing();
                }
            }
        }
    }
}