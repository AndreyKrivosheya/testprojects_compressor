using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class PayloadWriteBytesFinish: Payload
    {
        public PayloadWriteBytesFinish(CancellationTokenSource cancellationTokenSource, Stream stream, int streamOperationTimeoutMilliseconds, Func<Exception, Exception> exceptionProducer)
            : base(cancellationTokenSource, stream)
        {
            this.Timeout = streamOperationTimeoutMilliseconds;
            this.ExceptionProducer = exceptionProducer;
            if(this.ExceptionProducer == null)
            {
                this.ExceptionProducer = (e) => e;
            }
        }

        readonly int Timeout;
        readonly Func<Exception, Exception> ExceptionProducer;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((IAsyncResult writingAsyncResult) =>
                writingAsyncResult.WaitCompletedAndRunUnsafe(Timeout, CancellationTokenSource.Token,
                    whenCompleted: (writingCompletedAsyncResult) =>
                    {
                        try
                        {
                            Stream.EndWrite(writingCompletedAsyncResult);
                            return new PayloadResultContinuationPending();
                        }
                        catch(Exception e)
                        {
                            var eNew = ExceptionProducer(e);
                            if(eNew != null)
                            {
                                if(!object.ReferenceEquals(eNew, e))
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
                                return new PayloadResultContinuationPending();
                            }
                        }
                    }
                )
            );
        }
    }
}