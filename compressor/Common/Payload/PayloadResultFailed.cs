using System;
using System.Runtime.ExceptionServices;

namespace compressor.Common.Payload
{
    class PayloadResultFailed: PayloadResult
    {
        public PayloadResultFailed(ExceptionDispatchInfo failure)
            : base(PayloadResultStatus.Failed)
        {
            this.Failure = failure;         
        }
        public PayloadResultFailed(Exception failure)
            : this(ExceptionDispatchInfo.Capture(failure))
        {
        }

        public readonly ExceptionDispatchInfo Failure;
    }
}