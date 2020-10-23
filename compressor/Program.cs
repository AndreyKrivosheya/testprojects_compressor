using System;
using System.Linq;
using System.IO;

using compressor.Common;
using compressor.Processor.Settings;

namespace compressor
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("compressor.exe compress|decompress in out");
        }

        static ReturnCode RunProcessor(SettingsProvider settings, string pathIn, string pathOut, Func<SettingsProvider, Stream, Stream, Processor.Processor> processorFactory)
        {
            var
            stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            try
            {
                using(var inStream = new FileStream(pathIn, FileMode.Open))
                {
                    Stream outStream = null;
                    try
                    {
                        try
                        {
                            outStream = new FileStream(pathOut, FileMode.Open);
                            outStream.SetLength(0);
                        }
                        catch(FileNotFoundException)
                        {
                            outStream = new FileStream(pathOut, FileMode.CreateNew);
                        }
                    
                        var
                        processorToRun = processorFactory(settings, inStream, outStream);
                        processorToRun.Run();
                    }
                    finally
                    {
                        if(outStream != null)
                        {
                            outStream.Dispose();
                            outStream = null;
                        }
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                System.Diagnostics.Debug.WriteLine("Time took: '{0}'", stopWatch.Elapsed);
            }

            return ReturnCode.Success;
        }
        static ReturnCode RunProcessor(string pathIn, string pathOut, Func<SettingsProvider, Stream, Stream, Processor.Processor> processorfactory)
        {
            return RunProcessor(SettingsProviderFromEnvironment.Instance, pathIn, pathOut, processorfactory);
        }

        static ReturnCode MainWithTypedReturn(string[] args)
        {
            try
            {
                if(args.Length < 3 || args.Length > 3)
                {
                    Usage();
                    return ReturnCode.Error;
                }
                else
                {
                    if(string.Equals("compress", args[0], StringComparison.InvariantCultureIgnoreCase))
                    {
                        return RunProcessor(args[1], args[2], (settings, input, output) => new Processor.ProcessorParallelCompress(settings, input, output));
                    }
                    else if(string.Equals("decompress", args[0], StringComparison.InvariantCultureIgnoreCase))
                    {
                        return RunProcessor(args[1], args[2], (settings, input, output) => new Processor.ProcessorParallelDecompress(settings, input, output));
                    }
                    else
                    {
                        Console.WriteLine("Command '{0}' is unknown.", args[0]);
                        Usage();
                        return ReturnCode.Error;
                    }
                }
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                Console.WriteLine(e.GetMessagesChain());
                return ReturnCode.Error;
            }
        }
        static int Main(string[] args)
        {
            return (int)(MainWithTypedReturn(args));
        }
    }
}
