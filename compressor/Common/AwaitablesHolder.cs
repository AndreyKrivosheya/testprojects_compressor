using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Common
{
    interface AwaitablesHolder
    {
        IEnumerable<WaitHandle> GetAwaitables();
    }
}