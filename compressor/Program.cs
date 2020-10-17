using System;
using System.Linq;
using System.IO;
using System.IO.Compression;

namespace compressor
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("compressor.exe compress|decompress in out");
        }

        static ReturnCode RunProcessor<TaskFactoryReadWrite, TaskFactoryCompressDecompress>(string pathIn, string pathOut)
            where TaskFactoryReadWrite: IProcessorTaskFactoryReadWrite, new()
            where TaskFactoryCompressDecompress: IProcessorTaskFactoryCompressDecompress, new()
        {
            var
            stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            try
            {
                using(var inStream = new FileStream(pathIn, FileMode.Open))
                {
                    using(var outStream = new FileStream(pathOut, System.IO.FileMode.OpenOrCreate))
                    {
                        new Processor<TaskFactoryReadWrite, TaskFactoryCompressDecompress>(
                            new SettingsProviderFromEnvironment(), inStream, outStream).Run();
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
                if(args.Length < 3 || args.Length > 3)
                {
                    Usage();
                    return ReturnCode.Error;
                }

                var commands = new [] { 
                    Tuple.Create<Func<string, bool>, Func<string, string, string, ReturnCode>>(
                        (cmd) => string.Equals("compress", cmd, StringComparison.InvariantCultureIgnoreCase),
                        (cmd, pathIn, pathOut) => {
                            return RunProcessor<ProcessorTaskFactoryReadWriteFromBinaryToArchive, ProcessorTaskFactoryCompress>(
                                pathIn, pathOut);
                        } ),
                    Tuple.Create<Func<string, bool>, Func<string, string, string, ReturnCode>>(
                        (cmd) => string.Equals("decompress", cmd, StringComparison.InvariantCultureIgnoreCase),
                        (cmd, pathIn, pathOut) => {
                            return RunProcessor<ProcessorTaskFactoryReadWriteFromArchiveToBinary, ProcessorTaskFactoryDecompress>(
                                pathIn, pathOut);
                        } ),
                    Tuple.Create<Func<string, bool>, Func<string, string, string, ReturnCode>>(
                        (cmd) => true,
                        (cmd, fileIn, fileOut) => {
                            Console.WriteLine("Command '{0}' is unknown.", cmd);
                            Usage();
                            return ReturnCode.Error;
                         } )
                };
                var command = commands.First(x => x.Item1(args[0])).Item2;
                return command(args[0], args[1], args[2]);
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
