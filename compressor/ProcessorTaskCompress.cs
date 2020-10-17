using System;
using System.IO;
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
                    }

                    using(var outStream = new MemoryStream((int)outStreamRaw.Length - 10))
                    {
                        var outRawBytes = outStreamRaw.ToArray();
                        // all but the header
                        outStream.Write(outRawBytes, 10, outRawBytes.Length - 10);

                        return outStream.ToArray();
                    }
                }
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to compress block", e);
            }
             
        }
        public static ProcessorQueueBlock CompressBlock(ProcessorQueueBlock block)
        {
            return new ProcessorQueueBlock(block, CompressData(block.Data));
        }
    }
}