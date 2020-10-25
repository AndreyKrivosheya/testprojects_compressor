using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Convert
{
    abstract class PayloadBytes: Payload
    {
        public PayloadBytes(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource)
        {
        }

        protected abstract PayloadResult RunUnsafe(byte[] data);
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            var data = parameter as byte[];
            if(data == null)
            {
                throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not byte[]", parameter), "parameter");
            }

            return RunUnsafe(data);
        }
   }
}