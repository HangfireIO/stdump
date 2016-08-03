using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Diagnostics.Runtime;

namespace STDump
{
    public static class DumpHelper
    {
        public static DataTarget LoadOrAttach(string target, AttachFlag attachFlag, uint msecTimeout)
        {
            if (File.Exists(target))
            {
                return DataTarget.LoadCrashDump(target);
            }

            Process process = null;

            Int32 processId;
            if (Int32.TryParse(target, out processId))
            {
                process = Process.GetProcessById(processId);
            }
            else
            {
                var processes = Process.GetProcesses();
                var found = new List<Process>();
                // TODO: Access denied workaround

                foreach (var process1 in processes)
                {
                    if (process1.ProcessName.StartsWith(target))
                    {
                        found.Add(process1);
                    }
                }

                if (found.Count > 1)
                {
                    throw new ProcessNotFoundException("Multiple processes match the specified name.");
                }
                if (found.Count == 0)
                {
                    throw new ProcessNotFoundException("No process matching the specified name can be found. Try elevating the command prompt.");
                }

                process = found[0];
            }

            if (process == null)
            {
                throw new ProcessNotFoundException("No process matching the specified name can be found. Try elevating the command prompt.");
            }

            //Console.WriteLine($"Using {process.MainModule.FileName}");

            return DataTarget.AttachToProcess(process.Id, msecTimeout, attachFlag);
        }

        public static void WriteDump(DataTarget target, TextWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var clrVersion in target.ClrVersions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Environment.Is64BitProcess && clrVersion.DacInfo.TargetArchitecture == Architecture.X86)
                {
                    Console.WriteLine("Use stdump-x86.exe");
                    Environment.Exit(-2);
                }

                if (!Environment.Is64BitProcess && clrVersion.DacInfo.TargetArchitecture == Architecture.Amd64)
                {
                    Console.WriteLine("Use stdump.exe instead.");
                    Environment.Exit(-2);
                }

                writer.WriteLine($"Found CLR {clrVersion.Version}");
                writer.WriteLine();

                var runtime = clrVersion.CreateRuntime();

                writer.WriteLine("Following AppDomains found:");
                writer.WriteLine();

                foreach (var appDomain in runtime.AppDomains)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    DumpAppDomain(appDomain, writer);
                    writer.WriteLine();
                }

                writer.WriteLine("Following threads found:");
                writer.WriteLine();

                foreach (var thread in runtime.Threads)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!thread.IsAlive || thread.IsAborted || thread.IsUnstarted) continue;

                    DumpThreadInfo(thread, writer);
                    writer.WriteLine();

                    DumpCurrentException(thread, writer);
                    DumpStackTrace(thread, writer, cancellationToken);
                    writer.WriteLine();
                }
            }
        }

        private static void DumpAppDomain(ClrAppDomain appDomain, TextWriter writer)
        {
            writer.WriteLine($"AppDomain #{appDomain.Id}");
            writer.WriteLine($"  Address:       {appDomain.Address}");
            writer.WriteLine($"  Name:          {appDomain.Name}");
            writer.WriteLine($"  Configuration: {appDomain.ConfigurationFile ?? "n/a"}");
            writer.WriteLine($"  Directory:     {appDomain.ApplicationBase ?? "n/a"}");
        }

        private static void DumpThreadInfo(ClrThread thread, TextWriter writer)
        {
            writer.WriteLine($"Thread #{thread.ManagedThreadId}");
            writer.WriteLine($"  OS Thread ID:      {thread.OSThreadId}");
            writer.WriteLine($"  AppDomain Address: {thread.AppDomain}");

            var type = thread.IsBackground ? "Background" : "Foreground";
            writer.WriteLine($"  Type:              {type}");

            if (thread.IsAbortRequested)
                writer.WriteLine($"  IsAbortRequested:  {thread.IsAbortRequested}");

            var roles = new List<string>();
            if (thread.IsFinalizer) roles.Add("Finalizer");
            if (thread.IsDebuggerHelper) roles.Add("Debugger Helper");
            if (thread.IsGC) roles.Add("GC Thread");
            if (thread.IsShutdownHelper) roles.Add("Shutdown Helper");

            if (thread.IsThreadpoolCompletionPort) roles.Add("Threadpool I/O Completion Port");
            if (thread.IsThreadpoolGate) roles.Add("Threadpool Gate");
            if (thread.IsThreadpoolTimer) roles.Add("Threadpool Timer");
            if (thread.IsThreadpoolWait) roles.Add("Threadpool Wait");
            if (thread.IsThreadpoolWorker) roles.Add("Threadpool Worker");

            if (roles.Count > 0)
                writer.WriteLine($"  Role:              {String.Join(", ", roles)}");
        }

        private static void DumpCurrentException(ClrThread thread, TextWriter writer)
        {
            if (thread.CurrentException != null)
            {
                writer.WriteLine(thread.CurrentException);
            }
        }

        private static void DumpStackTrace(ClrThread thread, TextWriter writer, CancellationToken cancellationToken)
        {
            // TODO: StackTrace property may be clipped, add a note
            if (thread.StackTrace.Count > 0)
            {
                writer.WriteLine("  Managed stack trace:");

                foreach (var frame in thread.StackTrace)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.WriteLine($"   - {frame} at {frame.ModuleName}");
                }
            }
            else
            {
                writer.WriteLine("  No managed stack trace found for this thread.");
            }
        }
    }
}
