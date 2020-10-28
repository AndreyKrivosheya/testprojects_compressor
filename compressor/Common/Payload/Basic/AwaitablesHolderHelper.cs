using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    static class AwaitablesHolderHelper
    {
        public static IEnumerable<WaitHandle> GetAwaitables(this IEnumerable<Payload> input)
        {
            return input.OfType<AwaitablesHolder>().SelectMany(x => x.GetAwaitables());
        }

        public static IEnumerable<WaitHandle> GetAwaitables(this Payload input)
        {
            return (new [] { input }).GetAwaitables();
        }
    }
}