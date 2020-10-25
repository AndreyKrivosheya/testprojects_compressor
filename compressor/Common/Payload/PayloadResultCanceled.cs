namespace compressor.Common.Payload
{
    class PayloadResultCanceled: PayloadResult
    {
        public PayloadResultCanceled()
            : base(PayloadResultStatus.Canceled)
        {
        }
        public PayloadResultCanceled(object result)
            : base(PayloadResultStatus.Canceled, result)
        {
        }
    }
}