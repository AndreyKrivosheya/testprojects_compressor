using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadSucceed: Payload
    {
        public PayloadSucceed(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }

        protected override PayloadResult RunUnsafe(object parameter)
        {
            return new PayloadResultSucceeded(parameter);
        }
    }
}