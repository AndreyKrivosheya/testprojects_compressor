using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace compressor.Common.Payload
{
    abstract class Payload
    {
        public Payload(CancellationTokenSource cancellationTokenSource)
        {
            this.CancellationTokenSource = cancellationTokenSource;
        }

        protected readonly CancellationTokenSource CancellationTokenSource;
        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }

        protected abstract PayloadResult RunUnsafe(object parameter);
        public PayloadResult Run(object parameter)
        {
            try
            {
                return RunUnsafe(parameter);
            }
            catch(Exception e)
            {
                return new PayloadResultFailed(e);
            }
        }
        public PayloadResult Run()
        {
            return Run(null);
        }

        protected static PayloadResult ConvertAndRunUnsafe<T>(object parameter, Func<T, PayloadResult> runner)
        {
            T parameterCasted;
            try
            {
                parameterCasted = (T)parameter;
            }
            catch(InvalidCastException e)
            {
                throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not '{1}'", parameter, typeof(T)), "parameter", e);
            }

            return runner(parameterCasted);
        }
        protected static PayloadResult VerifyParameterNotNullConvertAndRunUnsafe<T>(object parameter, Func<T, PayloadResult> runner)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            return ConvertAndRunUnsafe(parameter, runner);
        }

        protected static PayloadResult ConvertAndRunUnsafe(object parameter, Func<int, PayloadResult> runner)
        {
            int parameterCasted;
            try
            {
                parameterCasted = System.Convert.ToInt32(parameter);;
            }
            catch(Exception e)
            {
                if(e is InvalidCastException || e is FormatException || e is OverflowException)
                {
                    throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not int", parameter), "parameter", e);
                }
                else
                {
                    throw;
                }
            }

            return runner(parameterCasted);
        }
        protected static PayloadResult VerifyParameterNotNullConvertAndRunUnsafe(object parameter, Func<int, PayloadResult> runner)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            return ConvertAndRunUnsafe(parameter, runner);
        }
    }
}