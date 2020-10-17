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
    }
}