using System;
using System.Linq;
using System.IO;
using System.IO.Compression;

namespace compressor
{
    class Program
    {
        const int returncode_error = 1;
        const int returncode_success = 0;

        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("compressor.exe compress|decompress in out");
        }


        static int Perform(Stream inStream, Stream outStream)
        {
            const int bufferSize = 1024 * 1024;
            const int queueSize = 1024;

            var queue = new System.Collections.Concurrent.BlockingCollection<Tuple<byte[], int>>(queueSize);
            var eventReadFinished = new System.Threading.ManualResetEvent(false);
            var eventWriteFinished = new System.Threading.ManualResetEvent(false);

            var readFailed = false;
            var writeStarted = false;
            var writeFailed = false;

            AsyncCallback
            writeCallback = null;
            writeCallback = (ar) => {
                try
                {
                    outStream.EndWrite(ar);
                    if(!readFailed)
                    {
                        Tuple<byte[], int> buffer = null;
                        var queueFinished = false;
                        try
                        {
                            buffer = queue.Take();
                        }
                        catch(InvalidOperationException)
                        {
                            queueFinished = true;
                        }
                        catch(OperationCanceledException)
                        {
                            queueFinished = true;    
                        }

                        if(!queueFinished)
                        {
                            //throw new ApplicationException("test");
                            outStream.BeginWrite(buffer.Item1, 0, buffer.Item2, writeCallback, null);
                        }
                        else
                        {
                            eventWriteFinished.Set();
                        }
                    }
                    else
                    {
                        eventWriteFinished.Set();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Writing to output failed:");
                    Console.WriteLine(e.Message);

                    queue.CompleteAdding();
                    writeFailed = true;
                    eventWriteFinished.Set();
                }
            };
            AsyncCallback
            readCallback = null;
            readCallback = (ar) => {
                try
                {
                    var totalRead = inStream.EndRead(ar);
                    if(!writeFailed)
                    {
                        var bufferRead = (byte[])ar.AsyncState;
                    
                        if(!writeStarted)
                        {
                            writeStarted = true;
                            outStream.BeginWrite(bufferRead, 0, totalRead, writeCallback, null);
                        }
                        else
                        {
                            try
                            {
                                queue.Add(Tuple.Create(bufferRead, totalRead));
                            }
                            catch(InvalidOperationException)
                            {
                                // writeFailed == true
                                eventReadFinished.Set();
                                return;
                            }
                        }
                        
                        if(totalRead == 0)
                        {
                            queue.CompleteAdding();
                            eventReadFinished.Set();
                        }
                        else
                        {
                            var buffer = new byte[bufferSize];
                            inStream.BeginRead(buffer, 0, buffer.Length, readCallback, buffer);
                        }
                    }
                    else
                    {
                        eventReadFinished.Set();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Failed to read from input:");
                    Console.WriteLine(e.Message);

                    readFailed = true;
                    queue.CompleteAdding();
                    eventReadFinished.Set();
                    if(!writeStarted)
                    {
                        eventWriteFinished.Set();
                    }
                }
            };

            var buffer = new byte[bufferSize];
            inStream.BeginRead(buffer, 0, buffer.Length, readCallback, buffer);

            eventReadFinished.WaitOne();
            inStream.Dispose();
            eventWriteFinished.WaitOne();
            outStream.Flush();
            outStream.Dispose();

            if(readFailed  || writeFailed)
            {
                return returncode_error;
            }
            else
            {
                return returncode_success;
            }
        }

        static int Main(string[] args)
        {
            try
            {
                if(args.Length < 3 || args.Length > 3)
                {
                    Usage();
                    return returncode_error;
                }

                var commands = new [] { 
                    Tuple.Create<Func<string, bool>, Func<string, string, string, int>>(
                        (cmd) => string.Equals("compress", cmd, StringComparison.InvariantCultureIgnoreCase),
                        (cmd, pathIn, pathOut) => { 
                            return Perform(
                                new FileStream(pathIn, FileMode.Open),
                                new GZipStream(new FileStream(pathOut, System.IO.FileMode.OpenOrCreate), CompressionMode.Compress));
                         } ),
                    Tuple.Create<Func<string, bool>, Func<string, string, string, int>>(
                        (cmd) => string.Equals("decompress", cmd, StringComparison.InvariantCultureIgnoreCase),
                        (cmd, pathIn, pathOut) => {
                            return Perform(
                                new GZipStream(new FileStream(args[1], FileMode.Open), CompressionMode.Decompress),
                                new FileStream(args[2], FileMode.OpenOrCreate));
                        } ),
                    Tuple.Create<Func<string, bool>, Func<string, string, string, int>>(
                        (cmd) => true,
                        (cmd, fileIn, fileOut) => {
                            Console.WriteLine("Command '{0}' is unknown.", cmd);
                            Usage();
                            return returncode_error;
                         } )
                };
                var command = commands.First(x => x.Item1(args[0])).Item2;
                return command(args[0], args[1], args[2]);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return returncode_error;
            }
        }
    }
}
