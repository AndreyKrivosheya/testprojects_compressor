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
    class PayloadBlocksToWriteToBytesBinary : PayloadBlocksToWriteToBytesViaDelegate
    {
        public PayloadBlocksToWriteToBytesBinary(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings, BlocksToBytes)
        {
        }

        static byte[] BlocksToBytes(List<BlockToWrite> blocks)
        {
            if(blocks.Count == 1)
            {
                return blocks[0].Data;
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
    }
}