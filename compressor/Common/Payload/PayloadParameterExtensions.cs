using System;

namespace compressor.Common.Payload
{
    static class PayloadParameterExtensions
    {
        public static PayloadResult ConvertAndRunUnsafe<T>(this object parameter, Func<T, PayloadResult> runner)
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
        public static PayloadResult VerifyNotNullConvertAndRunUnsafe<T>(this object parameter, Func<T, PayloadResult> runner)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            return parameter.ConvertAndRunUnsafe(runner);
        }

        public static PayloadResult ConvertAndRunUnsafe(this object parameter, Func<int, PayloadResult> runner)
        {
            int parameterCasted;
            try
            {
                parameterCasted = System.Convert.ToInt32(parameter);
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
        public static PayloadResult VerifyNotNullConvertAndRunUnsafe(this object parameter, Func<int, PayloadResult> runner)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            return parameter.ConvertAndRunUnsafe(runner);
        }
    }
}