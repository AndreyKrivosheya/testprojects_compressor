using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using compressor.Common;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadBlocksToWriteToBytesBinary : PayloadBlocksToWriteToBytes
    {
        public PayloadBlocksToWriteToBytesBinary(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource, BlocksToBytes)
        {
        }

        static byte[] BlocksToBytes(IEnumerable<BlockToWrite> blocks)
        {
            if(blocks.Any())
            {
                if(blocks.CountIsExactly(0))
                {
                    return blocks.First().Data;
                }
                else
                {
                    using(var blocksStream = new MemoryStream((int)(blocks.Select(x => x.Data.LongLength).Sum())))
                    {
                        foreach (var block in blocks)
                        {
                            blocksStream.Write(block.Data, 0, block.Data.Length);
                        }

                        return blocksStream.ToArray();
                    }
                }
            }
            else
            {
                return new byte[] {};
            }
        }
    }
}