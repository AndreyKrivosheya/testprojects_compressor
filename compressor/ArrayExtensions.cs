using System;

namespace compressor
{
    static class ArrayExtensions
    {
        public static T[] SubArray<T>(this T[] source, int start, int length)
        {
            var destination = new T[length];
            Array.Copy(source, start, destination, 0, length);

            return destination;
        }
        public static T[] SubArray<T>(this T[] source, int start)
        {
            return source.SubArray(start, source.Length - start);
        }

        public static T[] Concat<T>(this T[] one, T[] another)
        {
            var result = new T[one.Length + another.Length];
            Array.Copy(one, 0, result, 0, one.Length);
            Array.Copy(another, 0, result, one.Length, another.Length);
            return result;
        }
    }
}