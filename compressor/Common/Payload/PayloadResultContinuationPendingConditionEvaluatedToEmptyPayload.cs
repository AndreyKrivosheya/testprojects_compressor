namespace compressor.Common.Payload
{
    class PayloadResultContinuationPendingEvaluatedToEmptyPayload : PayloadResultContinuationPendingBase
    {
        public PayloadResultContinuationPendingEvaluatedToEmptyPayload()
            : base(ContinuationStatus.EvaluatedToEmptyPayload)
        {
        }
    }
}