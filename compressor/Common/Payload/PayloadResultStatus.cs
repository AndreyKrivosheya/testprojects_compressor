namespace compressor.Common.Payload
{
    enum PayloadResultStatus
    {
        Succeeded,
        Canceled,
        Failed,
        ContinuationPending,
        ContinuationPendingDoneNothing,
        ContinuationPendingEvaluatedToEmptyPayload
    }
}