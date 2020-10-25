using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadWriteBytesFinish: Payload
    {
        public PayloadWriteBytesFinish(CancellationTokenSource cancellationTokenSource, Stream stream, Func<Exception, Exception> exceptionProducer)
            : base(cancellationTokenSource, stream)
        {
            this.ExceptionProducer = exceptionProducer;
            if(this.ExceptionProducer == null)
            {
                this.ExceptionProducer = (e) => null;
            }
        }

        readonly Func<Exception, Exception> ExceptionProducer;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((IAsyncResult writingAsyncResult) =>
            {
                if(writingAsyncResult.IsCompleted)
                {
                    try
                    {
                        Stream.EndWrite(writingAsyncResult);
                        return new PayloadResultContinuationPending();
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