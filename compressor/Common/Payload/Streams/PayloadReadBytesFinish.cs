using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    abstract class PayloadReadBytesFinish: Payload
    {
        public PayloadReadBytesFinish(CancellationTokenSource cancellationTokenSource, Stream stream, int streamOperationTimeoutMilleseconds)
            : base(cancellationTokenSource, stream)
        {
            this.Timeout = streamOperationTimeoutMilleseconds;
        }

        readonly int Timeout;

        protected abstract PayloadResult RunUnsafe(IAsyncResult completedReadingAsyncResult);
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((IAsyncResult readingAsyncResult) =>
            {
                if(readingAsyncResult.IsCompleted)
                {
                    return RunUnsafe(readingAsyncResult);
                }
                else
                {
                    if(Timeout != 0)
                    {
                        switch(WaitHandle.WaitAny(new [] { readingAsyncResult.AsyncWaitHandle, CancellationTokenSource.Token.WaitHandle }, Timeout))
                        {
                            case WaitHandle.WaitTimeout:
                                return new PayloadResultContinuationPendingDoneNothing();
                            case 0:
                                return RunUnsafe(readingAsyncResult);
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