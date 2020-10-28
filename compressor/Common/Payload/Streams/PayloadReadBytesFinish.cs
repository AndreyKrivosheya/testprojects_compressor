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
            return parameter.VerifyNotNullConvertAndRunUnsafe(
            (IAsyncResult readingAsyncResult) =>
            {
                return readingAsyncResult.WaitCompleted(Timeout, CancellationTokenSource.Token,
                    whenWaitTimedOut:
                        (incompleteAsyncResult) => new PayloadResultContinuationPendingDoneNothing(),
                    whenCompleted:
                        (completedAsyncResult) => RunUnsafe(completedAsyncResult)
                );
            });
        }
    }
}