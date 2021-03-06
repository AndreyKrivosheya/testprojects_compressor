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
    class PayloadBlocksToWriteToBytesArchive : PayloadBlocksToWriteToBytes
    {
        public PayloadBlocksToWriteToBytesArchive(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource, BlocksToBytes)
        {
        }

        static byte[] BlocksToBytes(IEnumerable<BlockToWrite> blocks)
        {
            if(blocks.Any())
            {
                if(blocks.CountIsExactly(1))
                {
                    var block = blocks.First();
                    return ArrayExtensions.Concat(BitConverter.GetBytes(block.Data.LongLength), block.Data);
                }
                else
                {
                    using(var blocksStream = new MemoryStream((int)(blocks.Select(x => x.Data.LongLength + sizeof(long)).Sum())))
                    {
                        using(var blocksStreamWriter = new BinaryWriter(blocksStream))
                        {
                            foreach (var block in blocks)
                            {
                                blocksStreamWriter.Write(block.Data.LongLength);
                                blocksStreamWriter.Write(block.Data);
                            }
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