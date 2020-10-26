using System;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadReturnValue: Payload
    {
        public PayloadReturnValue(CancellationTokenSource cancellationTokenSource, Func<object, object> valueProvider)
            : base(cancellationTokenSource)
        {
            this.ValueProvider = valueProvider;
        }
        public PayloadReturnValue(CancellationTokenSource cancellationTokenSource, Func<object> valueProvider)
            : this(cancellationTokenSource, valueProvider == null ? ((Func<object, object>)((parameter) => null)) : ((Func<object, object>)((parameter) => valueProvider())))
        {
        }
        public PayloadReturnValue(CancellationTokenSource cancellationTokenSource, object value)
            : this(cancellationTokenSource, () => value)
        {
        }

        readonly Func<object, object> ValueProvider;
        
        protected override PayloadResult RunUnsafe(object parameter)
        {
            return new PayloadResultContinuationPending(ValueProvider(parameter));
        }
    }
}