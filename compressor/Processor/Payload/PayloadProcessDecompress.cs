using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

using compressor.Common;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    class PayloadProcessDecompress : PayloadProcess
    {
        public PayloadProcessDecompress(CancellationTokenSource cancellationTokenSource)
            : base(cancellationTokenSource, DecompressBlock)
        {
        }

        public static byte[] DecompressData(byte[] data)
        {
            try
            {
                using(var inStream = new GZipStream(new MemoryStream(ArrayExtensions.Concat(GZipStreamHelper.Header, data)), CompressionMode.Decompress))
                {
                    using(var outStream = new MemoryStream(BitConverter.ToInt32(data, data.Length - sizeof(Int32))))
                    {
                        inStream.CopyTo(outStream);
                        return outStream.ToArray();
                    }
                }
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to decompress block", e);
            }
        }
        public static BlockToWrite DecompressBlock(BlockToProcess block)
        {
            var dataDecompressed = DecompressData(block.Data);
            if(dataDecompressed.Length != block.OriginalLength)
            {
                throw new ApplicationException("Failed to decompress block: decompressed size does not match with original one");
            }
            else
            {
                return new BlockToWrite(block, dataDecompressed);
            }
        }
    }
}