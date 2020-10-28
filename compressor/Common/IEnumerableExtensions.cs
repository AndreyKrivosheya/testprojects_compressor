using System;
using System.Collections.Generic;

namespace compressor.Common
{
    static class IEnumerableExtesnions
    {
        public static bool CountIsExactly<T>(this IEnumerable<T> enumerable, int n)
        {
            return enumerable.CountIsExactly((ulong)n);
        }
        public static bool CountIsExactly<T>(this IEnumerable<T> enumerable, uint n)
        {
            return enumerable.CountIsExactly((ulong)n);
        }
        public static bool CountIsExactly<T>(this IEnumerable<T> enumerable, ulong n)
        {
            var count = 0ul;
            foreach(var item in enumerable)
            {
                count++;
                if(count > n)
                {
                    return false;
                }
            }

            if(count < n)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}