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
        }

        readonly Func<Exception, Exception> ExceptionProducer;

        protected virtual PayloadResult RunUnsafe(IAsyncResult writingAsyncResult)
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
                    throw new ApplicationException("Failed to write block", e);
                }
            }

            return new PayloadResultContinuationPendingDoneNothing();
        }
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            var writingAsyncResult = parameter as IAsyncResult;
            if(writingAsyncResult == null)
            {
                throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not IAsyncResult", parameter), "parameter");
            }

            return RunUnsafe(writingAsyncResult);
        }
    }
}