using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadReadBytesNoMoreThenFinish: PayloadReadBytesFinish
    {
        public PayloadReadBytesNoMoreThenFinish(CancellationTokenSource cancellationTokenSource, Stream stream, int streamOperationTimeoutMilliseconds, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
            : base(cancellationTokenSource, stream, streamOperationTimeoutMilliseconds)
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