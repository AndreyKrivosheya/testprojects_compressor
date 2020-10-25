namespace compressor.Common.Payload
{
    class PayloadResultContinuationPendingDoneNothing : PayloadResultContinuationPendingBase
    {
        public PayloadResultContinuationPendingDoneNothing()
            : base(ContinuationStatus.DoneNothing)
        {
        }
    }
}