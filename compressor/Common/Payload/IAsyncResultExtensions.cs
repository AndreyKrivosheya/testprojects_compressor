using System;
using System.Threading;

namespace compressor.Common.Payload
{
    static class IAsyncResultExtensions
    {
        public static T WaitCompleted<T>(this IAsyncResult asyncResult, int waitTimeout, CancellationToken cancellationToken, Func<IAsyncResult, T> whileWaitTimedOut, Func<IAsyncResult, T> whenCompleted)
        {
            if(asyncResult.IsCompleted)
            {
                return whenCompleted(asyncResult);
            }
            else
            {
                if(waitTimeout != 0)
                {
                    switch(WaitHandle.WaitAny(new [] { asyncResult.AsyncWaitHandle, cancellationToken.WaitHandle }, waitTimeout))
                    {
                        case WaitHandle.WaitTimeout:
                            return whileWaitTimedOut(asyncResult);
                        case 0:
                            return whenCompleted(asyncResult);
                        case 1:
                        default:
                            throw new OperationCanceledException();
                    }
                }
                else
                {
                    return whileWaitTimedOut(asyncResult);
                }
            }
        }
        public static PayloadResult WaitCompletedAndRunUnsafe(this IAsyncResult asyncResult, int waitTimeout, CancellationToken cancellationToken, Func<IAsyncResult, PayloadResult> whenCompleted)
        {
            return asyncResult.WaitCompleted<PayloadResult>(waitTimeout, cancellationToken,
                whileWaitTimedOut:
                    (incompleteAsyncResult) => new PayloadResultContinuationPendingDoneNothing(),
                whenCompleted:
                    (completedAsyncResult) => whenCompleted(completedAsyncResult)
            );
        }
    }
}