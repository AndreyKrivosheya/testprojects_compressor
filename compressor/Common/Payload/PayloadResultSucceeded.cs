namespace compressor.Common.Payload
{
    class PayloadResultSucceeded: PayloadResult
    {
        public PayloadResultSucceeded()
            : base(PayloadResultStatus.Succeeded)
        {
        }
        public PayloadResultSucceeded(object result)
            : base(PayloadResultStatus.Succeeded, result)
        {
        }
    }
}