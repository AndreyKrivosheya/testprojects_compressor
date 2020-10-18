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
                using(var inStream = new MemoryStream(10 + data.Length))
                {
                    // header
                    inStream.Write(new byte[] { 0x1f, 0x8b, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0a }, 0, 10);
                    // data
                    inStream.Write(data, 0, data.Length);
                    
                    inStream.Seek(0, SeekOrigin.Begin);
                    using(var inStreamGz = new GZipStream(inStream, CompressionMode.Decompress))
                    {
                        using(var outStream = new MemoryStream())
                        {
                            inStreamGz.CopyTo(outStream);
                            return outStream.ToArray();
                        }
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