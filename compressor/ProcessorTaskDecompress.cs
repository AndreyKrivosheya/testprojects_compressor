using System;
using System.IO;
using System.IO.Compression;

namespace compressor
{
    class ProcessorTaskDecompress: ProcessorTaskCompressDescompress
    {
        public ProcessorTaskDecompress(ISettingsProvider settings)
            : base(settings, DecompressBlock)
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
        public static ProcessorQueueBlockToWrite DecompressBlock(ProcessorQueueBlockToProcess block)
        {
            var dataDecompressed = DecompressData(block.Data);
            if(dataDecompressed.Length != block.OriginalLength)
            {
                throw new ApplicationException("Failed to decompress block: decompressed size does not match with original one");
            }
            else
            {
                return new ProcessorQueueBlockToWrite(block, dataDecompressed);
            }
        }
    }
}