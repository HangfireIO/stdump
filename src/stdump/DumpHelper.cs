using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Diagnostics.Runtime;

namespace STDump
{
    public static class DumpHelper
    {
        public static DataTarget LoadOrAttach(string target, uint msecTimeout)
        {
            if (File.Exists(target))
            {
                return DataTarget.LoadDump(target);
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
            return DataTarget.AttachToProcess(process.Id, true);
        }

        public static void WriteDump(DataTarget target, TextWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var clrVersion in target.ClrVersions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                writer.WriteLine($"Found CLR Version: {clrVersion.Version}");
                var architecture = clrVersion.DebuggingLibraries.FirstOrDefault()?.TargetArchitecture;
                writer.WriteLine($"Found Target Architecture: {architecture}");

                if (architecture != null)
                {
                    if (Environment.Is64BitProcess && architecture.Value == Architecture.X86)
                    {
                        Console.WriteLine("Use stdump-x86.exe");
                        Environment.Exit(-2);
                    }

                    if (!Environment.Is64BitProcess && architecture.Value == Architecture.X64)
                    {
                        Console.WriteLine("Use stdump.exe instead.");
                        Environment.Exit(-2);
                    }
                }

                writer.WriteLine();

                var runtime = clrVersion.CreateRuntime();

                if (runtime.ThreadPool != null)
                {
                    writer.WriteLine("Following ThreadPool information found:");
                    writer.WriteLine($"  CPU Utilization by ThreadPool: {runtime.ThreadPool.CpuUtilization}%");
                    writer.WriteLine($"  Worker threads: Active {runtime.ThreadPool.ActiveWorkerThreads}, Idle {runtime.ThreadPool.IdleWorkerThreads}, Min {runtime.ThreadPool.MinThreads}, Max {runtime.ThreadPool.MaxThreads}");
                    writer.WriteLine($"  Completion ports: Free {runtime.ThreadPool.FreeCompletionPorts}, Total {runtime.ThreadPool.TotalCompletionPorts}, Min {runtime.ThreadPool.MinCompletionPorts}, Max {runtime.ThreadPool.MaxCompletionPorts}");
                    string type = null;
                    if (runtime.ThreadPool.UsingPortableThreadPool) type = "Portable";
                    if (runtime.ThreadPool.UsingWindowsThreadPool) type = "Windows";
                    writer.WriteLine($"  Type: {type}");
                }
                else
                {
                    writer.WriteLine("No ThreadPool information found.");
                }

                writer.WriteLine();

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

                    if (!thread.IsAlive || (thread.State & ClrThreadState.TS_Aborted) != 0 ||
                        (thread.State & ClrThreadState.TS_Unstarted) != 0) continue;

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
            writer.WriteLine($"  AppDomain Address: {thread.CurrentAppDomain?.Address}");
            writer.WriteLine($"  State:             {thread.State:G}");

            var roles = new List<string>();
            if (thread.IsFinalizer) roles.Add("Finalizer");
            if (thread.IsGc) roles.Add("GC");

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
            var stackTrace = thread.EnumerateStackTrace().ToArray();

            if (stackTrace.Length > 1)
            {
                writer.WriteLine("  Managed stack trace:");

                foreach (var frame in stackTrace)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var moduleName = frame.Method?.Type.Module?.Name;
                    if (moduleName != null) moduleName = Path.GetFileName(moduleName);

                    writer.WriteLine($"   - {frame} at {moduleName}");
                }
            }
            else
            {
                writer.WriteLine("  No managed stack trace found for this thread.");
            }
        }
    }
}
