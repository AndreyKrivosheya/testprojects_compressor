using System;

namespace compressor
{
    class SettingsProviderFromEnvironment : ISettingsProvider
    {
        private readonly Lazy<int> MaxConcurrencyLazy;
        public int MaxConcurrency
        {
            get
            {
                return MaxConcurrencyLazy.Value;
            }
        }

        private readonly Lazy<int> MaxQueueSizeLazy;
        public int MaxQueueSize
        {
            get
            {
                return MaxQueueSizeLazy.Value;
            }
        }

        private readonly Lazy<long> BlockSizeLazy;
        public long BlockSize
        {
            get
            {
                return BlockSizeLazy.Value;
            }
        }
        public SettingsProviderFromEnvironment()
        {
            BlockSizeLazy = new Lazy<long>(() => {
                    var def = 1L * 1024 * 1024;
                    var value = ReadFromEnvironmentVariableAndConvertToLong("COMPRESSOR_BLOCK_SIZE", def);
                    return Math.Min(value >= 1 ? value : def, int.MaxValue);
                });

            MaxConcurrencyLazy = new Lazy<int>(() => {
                    var def = Environment.ProcessorCount;
                    var value = ReadFromEnvironmentVariableAndConvertToInt("COMPRESSOR_MAX_CONCURRENCY", def);
                    return value >= 1 ? value : def;
                });
            MaxQueueSizeLazy = new Lazy<int>(() => {
                    var def = 100;
                    var value = ReadFromEnvironmentVariableAndConvertToInt("COMPRESSOR_MAX_QUEUE_SIZE", def);
                    return Math.Max(Math.Min(value >= 1 ? value : def, (int)(2L * 1024 * 1024 * 1024 / BlockSize)), 1);
                });
        }

        protected string ReadFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);
            if(string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                return Environment.ExpandEnvironmentVariables(value);
            }
        }
        protected T ReadFromEnvironmentVariableAndConvertToT<T>(string environmentVariableName, T defaultValue, Func<string, T> convertor)
        {
            var value = ReadFromEnvironmentVariable(environmentVariableName);
            if(string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    return convertor(value);
                }
                catch(Exception)
                {
                    return defaultValue;
                }
            }
        }
        protected long ReadFromEnvironmentVariableAndConvertToLong(string environmentVariableName, long defaultValue)
        {
            return ReadFromEnvironmentVariableAndConvertToT(environmentVariableName, default, (s) => long.Parse(s));
        }
        protected int ReadFromEnvironmentVariableAndConvertToInt(string environmentVariableName, int defaultValue)
        {
            return ReadFromEnvironmentVariableAndConvertToT(environmentVariableName, defaultValue, (s) => int.Parse(s));
        }

    }
}