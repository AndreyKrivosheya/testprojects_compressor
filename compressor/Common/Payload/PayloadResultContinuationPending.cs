namespace compressor.Common.Payload
{
    class PayloadResultContinuationPending : PayloadResultContinuationPendingBase
    {
        public PayloadResultContinuationPending(object result)
            : base(ContinuationStatus.DoneSomething, result)
        {
        }
        public PayloadResultContinuationPending()
            : this(null)
        {
        }
    }
}