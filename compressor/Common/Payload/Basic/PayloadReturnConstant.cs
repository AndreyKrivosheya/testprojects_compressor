using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadReturnConstant: Payload
    {
        public PayloadReturnConstant(CancellationTokenSource cancellationTokenSource, object constant)
            : base(cancellationTokenSource)
        {
            this.Constant = constant;
        }

        readonly object Constant;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            return new PayloadResultSucceeded(Constant);
        }
    }
}