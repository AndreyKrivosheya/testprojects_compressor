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

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (IAsyncResult writingAsyncResult) =>
            {
                return writingAsyncResult.WaitCompleted<PayloadResult>(Timeout, CancellationTokenSource.Token,
                    whenWaitTimedOut:
                        (incompleteAsyncResult) => new PayloadResultContinuationPendingDoneNothing(),
                    whenCompleted:
                        (completedAsyncResult) =>
                        {
                            try
                            {
                                Stream.EndWrite(completedAsyncResult);
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
                );
            });
        }
    }
}