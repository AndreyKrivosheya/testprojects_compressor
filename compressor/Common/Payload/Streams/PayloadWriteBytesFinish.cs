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
                this.ExceptionProducer = (e) => null;
            }
        }

        readonly int Timeout;
        readonly Func<Exception, Exception> ExceptionProducer;

        protected virtual PayloadResult RunUnsafe(IAsyncResult completedWritingAsyncResult)
        {
            try
            {
                Stream.EndWrite(completedWritingAsyncResult);
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
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((IAsyncResult writingAsyncResult) =>
            {
                if(writingAsyncResult.IsCompleted)
                {
                    return RunUnsafe(writingAsyncResult);
                }
                else
                {
                    if(Timeout != 0)
                    {
                        switch(WaitHandle.WaitAny(new [] { writingAsyncResult.AsyncWaitHandle, CancellationTokenSource.Token.WaitHandle }, Timeout))
                        {
                            case WaitHandle.WaitTimeout:
                                return new PayloadResultContinuationPendingDoneNothing();
                            case 0:
                                return RunUnsafe(writingAsyncResult);
                            case 1:
                            default:
                                throw new OperationCanceledException();
                        }
                    }
                    else
                    {
                        return new PayloadResultContinuationPendingDoneNothing();
                    }
                }
            });
        }
    }
}