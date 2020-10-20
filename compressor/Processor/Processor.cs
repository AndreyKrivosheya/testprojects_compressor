using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

using compressor.Common;
using compressor.Processor.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class Processor: Common.Processor
    {
        public Processor(SettingsProvider settings, Stream inputStream, Stream outputStream)
        {
            if(settings == null)
            {
                this.Settings = SettingsProviderFromEnvironment.Instance;
            }
            else
            {
                this.Settings = settings;
            }

            if(inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }
            this.InputStream = inputStream;

            if(outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }
            this.OutputStream = outputStream;
        }

        protected readonly SettingsProvider Settings;

        protected readonly Stream InputStream;
        protected readonly Stream OutputStream;
    }
}