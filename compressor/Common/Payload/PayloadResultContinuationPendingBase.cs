namespace compressor.Common.Payload
{
    abstract class PayloadResultContinuationPendingBase : PayloadResult
    {
        protected enum ContinuationStatus
        {
            DoneSomething,
            DoneNothing
        }
        protected PayloadResultContinuationPendingBase(ContinuationStatus status, object result)
            : base(ContinuationStatusToResultStatus(status), result)
        {
        }
        protected PayloadResultContinuationPendingBase(ContinuationStatus status)
            : this(status, null)
        {

        }

        static PayloadResultStatus ContinuationStatusToResultStatus(ContinuationStatus status)
        {
            switch(status)
            {
                case ContinuationStatus.DoneSomething:
                    return PayloadResultStatus.ContinuationPending;
                case ContinuationStatus.DoneNothing:
                default:
                    return PayloadResultStatus.ContinuationPendingDoneNothing;
            }
        }
    }
}