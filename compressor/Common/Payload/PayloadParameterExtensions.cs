using System;

namespace compressor.Common.Payload
{
    static class PayloadParameterExtensions
    {
        public static T VerifyNotNullAnd<T>(this object parameter, Func<object, T> transform)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            return transform(parameter);
        }

        public static U ConvertAnd<T, U>(this object parameter, Func<T, U> transform)
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

            return transform(parameterCasted);
        }
        public static PayloadResult ConvertAndRunUnsafe<T>(this object parameter, Func<T, PayloadResult> runner)
        {
            return parameter.ConvertAnd(runner);
        }

        public static U VerifyNotNullConvertAnd<T, U>(this object parameter, Func<T, U> transform)
        {
            return parameter.VerifyNotNullAnd((parameter) => parameter.ConvertAnd(transform));
        }
        public static PayloadResult VerifyNotNullConvertAndRunUnsafe<T>(this object parameter, Func<T, PayloadResult> runner)
        {
            return parameter.VerifyNotNullConvertAnd(runner);
        }

        public static U ConvertAnd<U>(this object parameter, Func<int, U> transform)
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

            return transform(parameterCasted);
        }
        public static PayloadResult ConvertAndRunUnsafe(this object parameter, Func<int, PayloadResult> runner)
        {
            return parameter.ConvertAnd(runner);
        }

        public static U VerifyNotNullConvertAnd<U>(this object parameter, Func<int, U> transform)
        {
            return parameter.VerifyNotNullAnd((parameter) => parameter.ConvertAnd(transform));
        }
        public static PayloadResult VerifyNotNullConvertAndRunUnsafe(this object parameter, Func<int, PayloadResult> runner)
        {
            return parameter.VerifyNotNullConvertAnd(runner);
        }
    }
}