using System;
using System.IO;
using System.Linq;
using System.IO.Compression;

namespace compressor
{
    class ProcessorTaskCompress: ProcessorTaskCompressDescompress
    {
        public ProcessorTaskCompress(ISettingsProvider settings)
            : base(settings, CompressBlock)
        {
        }

        public static byte[] CompressData(byte[] data)
        {
            try
            {
                using(var outStreamRaw = new MemoryStream())
                {
                    using(var outStreamRawGz = new GZipStream(outStreamRaw, CompressionLevel.Optimal, true))
                    {
                        using(var inStream = new MemoryStream(data))
                        {
                            inStream.CopyTo(outStreamRawGz);
                        }
                        outStreamRawGz.Flush();
                    }

                    if(outStreamRaw.Length >= GZipStreamHelper.Header.Length)
                    {
                        if(outStreamRaw.Length == GZipStreamHelper.Header.Length)
                        {
                            return Enumerable.Empty<byte>().ToArray();
                        }
                        else
                        {
                            // all bytes but the header
                            return outStreamRaw.ToArray().SubArray(GZipStreamHelper.Header.Length);
                        }
                    }
                    else
                    {
                        throw new ApplicationException("Failed to compress block: compressed block size if less than header size");
                    }
                }
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to compress block", e);
            }
             
        }
        public static ProcessorQueueBlockToWrite CompressBlock(ProcessorQueueBlockToProcess block)
        {
            return new ProcessorQueueBlockToWrite(block, CompressData(block.Data));
        }
    }
}