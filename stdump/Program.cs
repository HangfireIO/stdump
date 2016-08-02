using System;
using System.IO;
using System.Threading;
using Microsoft.Diagnostics.Runtime;

namespace STDump
{
    public class Program
    {
        private const uint AttachProcessTimeout = 10 * 1000;
        private const AttachFlag AttachProcessFlag = AttachFlag.Passive;

        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();

        public static int Main(string[] args)
        {
            SubscribeCancelKeyPress();
            WriteHeader();

            if (args.Length != 1 || 
                args[0].Equals("/?", StringComparison.OrdinalIgnoreCase) || 
                args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
            {
                WriteUsage();
                return (int)ExitCode.UsageRequested;
            }

            try
            {
                using (var target = DumpHelper.LoadOrAttach(args[0], AttachProcessFlag, AttachProcessTimeout))
                {
                    DumpHelper.WriteDump(target, Console.Out, Cts.Token);
                }
            }
            catch (ClrDiagnosticsException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine("Try elevating the command prompt.");

                return (int) ExitCode.DiagnosticFailed;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return (int) ExitCode.TargetNotFound;
            }
            catch (ProcessNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return (int) ExitCode.TargetNotFound;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("The operation was canceled by the user.");
                return (int) ExitCode.Canceled;
            }

            return (int)ExitCode.Success;
        }

        private static void WriteUsage()
        {
            Console.Out.WriteLine("USAGE: stdump.exe process|pid|minidump");
        }

        private static void WriteHeader()
        {
            Console.Out.WriteLine("STDump v0.1 - Writes process managed stack traces");
            Console.Out.WriteLine("Copyright (C) 2016 Sergey Odinokov");
            Console.Out.WriteLine("Based on Microsoft.Diagnostics.Runtime (CLR MD). Copyright (C) Microsoft Corporation.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Dumps managed stack trace of a running process, or from a minidump file.");
            Console.Out.WriteLine();
        }
        
        private static void SubscribeCancelKeyPress()
        {
            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                Cts.Cancel();
            };
        }
    }
}
