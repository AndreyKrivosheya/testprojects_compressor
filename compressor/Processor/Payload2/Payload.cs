using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    abstract class Payload: Common.Payload.Payload
    {
        public Payload(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource)
        {
            this.Settings = settings;
        }

        protected readonly SettingsProvider Settings;
    }
}