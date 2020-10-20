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

        static ReturnCode RunProcessor<Factory>(string pathIn, string pathOut)
            where Factory: ProcessorFactory, new()
        {
            return RunProcessor<Factory>(SettingsProviderFromEnvironment.Instance, pathIn, pathOut);
        }
        static ReturnCode RunProcessor<Factory>(SettingsProvider settings, string pathIn, string pathOut)
            where Factory: ProcessorFactory, new()
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
                    
                        new Factory().Create(settings, inStream, outStream).Run();
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
        static ReturnCode MainWithTypedReturn(string[] args)
        {
            try
            {
                return new[] {
                    Tuple.Create<Func<string[], bool>, Func<string[], ReturnCode>>(
                        (arguments) => arguments.Length < 3 || arguments.Length > 3,
                        (arguments) => {
                            Usage();
                            return ReturnCode.Error;
                        }),
                    Tuple.Create<Func<string[], bool>, Func<string[], ReturnCode>>(
                        (arguments) => true,
                        (arguments) => {
                            return new [] { 
                                Tuple.Create<Func<string, bool>, Func<string, string, string, ReturnCode>>(
                                    (cmd) => string.Equals("compress", cmd, StringComparison.InvariantCultureIgnoreCase),
                                    (cmd, pathIn, pathOut) => RunProcessor<ProcessorFactoryToArchive>(pathIn, pathOut)),
                                Tuple.Create<Func<string, bool>, Func<string, string, string, ReturnCode>>(
                                    (cmd) => string.Equals("decompress", cmd, StringComparison.InvariantCultureIgnoreCase),
                                    (cmd, pathIn, pathOut) => RunProcessor<ProcessorFactoryFromArchive>(pathIn, pathOut)),
                                Tuple.Create<Func<string, bool>, Func<string, string, string, ReturnCode>>(
                                    (cmd) => true,
                                    (cmd, fileIn, fileOut) => {
                                        Console.WriteLine("Command '{0}' is unknown.", cmd);
                                        Usage();
                                        return ReturnCode.Error;
                                    } )
                            }.First(x => x.Item1(arguments[0])).Item2(arguments[0], arguments[1], arguments[2]);
                        })
                }.First(x => x.Item1(args)).Item2(args);
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
