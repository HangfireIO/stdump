using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.CommandLineUtils;

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

            var app = new CommandLineApplication(false)
            {
                Name = "stdump",
                FullName = "Stack Trace Dump",
                Description = "Writes managed stack trace of a running process, or from a minidump file to console."
            };

            var versions = GetShortAndLongVersion();

            app.Option("-p|--pause", "Pause process", CommandOptionType.NoValue);
            app.Option("-o|--output", "Specify a file", CommandOptionType.SingleValue);
            app.HelpOption("-h|--help");
            app.VersionOption("-v|--version", versions.Item1, versions.Item2);

            var arg = app.Argument("<process> or <dump>", "Process ID, process name or a dump file", true);
            
            app.OnExecute(() =>
            {
                if (arg.Values.Count == 0)
                {
                    app.ShowHelp();
                    return 1;
                }

                app.ShowRootCommandFullNameAndVersion();
                
                return DumpStackTraces(arg.Values);
            });

            return app.Execute(args);
        }

        private static int DumpStackTraces(IEnumerable<string> arguments)
        {
            try
            {
                foreach (var argument in arguments)
                {
                    Cts.Token.ThrowIfCancellationRequested();

                    using (var writer = new StringWriter())
                    {
                        using (var target = DumpHelper.LoadOrAttach(argument, AttachProcessFlag, AttachProcessTimeout))
                        {
                            DumpHelper.WriteDump(target, writer, Cts.Token);
                        }

                        Console.Out.Write(writer);
                    }
                }

                return 0;
            }
            catch (ClrDiagnosticsException ex)
            {
                Console.WriteLine("Error: " + ex.Message + " Try elevating the command prompt.");
                // TODO: Add bitness information

                return (int)ExitCode.DiagnosticFailed;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return (int)ExitCode.TargetNotFound;
            }
            catch (ProcessNotFoundException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return (int)ExitCode.TargetNotFound;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Error: The operation was canceled by the user.");
                return (int)ExitCode.Canceled;
            }
        }

        private static Tuple<string, string> GetShortAndLongVersion()
        {
            Func<TypeInfo, string> getVersionFromTypeInfo = typeInfo =>
            {
                var infoVersion = typeInfo.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
                    ?? typeInfo.Assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;

                if (string.IsNullOrEmpty(infoVersion))
                {
                    infoVersion = typeInfo.Assembly.GetName().Version.ToString();
                }

                return infoVersion;
            };

            var programVersion = getVersionFromTypeInfo(typeof(Program).GetTypeInfo());
            var clrMdVersion = getVersionFromTypeInfo(typeof(DataTarget).GetTypeInfo());

            return new Tuple<string, string>(
                programVersion,
                $"{programVersion} (ClrMD: {clrMdVersion})");
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
