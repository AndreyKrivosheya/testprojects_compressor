using System;
using System.Threading;

namespace compressor.Common
{
    static class IAsyncResultExtensions
    {
        public static void WaitCompleted(this IAsyncResult asyncResult)
        {
            if(!asyncResult.IsCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
            }
        }
    }
}