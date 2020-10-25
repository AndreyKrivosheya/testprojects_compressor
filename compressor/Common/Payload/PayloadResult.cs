namespace compressor.Common.Payload
{
    abstract class PayloadResult
    {
        public PayloadResult(PayloadResultStatus status, object result)
        {
            this.Status = status;
            this.Result = result;
        }
        public PayloadResult(PayloadResultStatus status)
            : this(status, null)
        {
        }

        public readonly PayloadResultStatus Status;
        public readonly object Result;
    }
}