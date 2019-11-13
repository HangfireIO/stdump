# stdump

Explore stack trace of a running managed process without downtime, or from a minidump file 

## Installation

You can install `stdump` using one of the following ways:

* **Manual**: Obtain an archive from the [latest release](https://github.com/odinserj/stdump/releases/latest).
* **NuGet**: `Install-Package stdump`
* **Chocolatey**: `choco install stdump`
* **.NET Core**: `dotnet tool install -g stdump`
 
After installing, there will be two executables: `stdump-x86.exe` and `stdump-x64.exe`.

## Usage

To display a stack trace of a running program, use its PID or process name.

```
stdump w3wp.exe
```

Or point `stdump` to a minidump file.

```
stdump c:\debug\w3wp.dmp
```

## Output

```
STDump v0.0.1 - Writes process managed stack traces
Copyright (C) 2016 Sergey Odinokov
Based on Microsoft.Diagnostics.Runtime (CLR MD). Copyright (C) Microsoft Corporation.

Writes managed stack trace of a running process, or from a minidump file to console.

Found CLR v4.6.1080.00

Following AppDomains found:

AppDomain #1
  Address:       5029808
  Name:          ConsoleApplication38.exe
  Configuration: c:\users\odinserj\documents\visual studio 2015\Projects\ConsoleApplication38\ConsoleApplication38\bin\Debug\ConsoleApplication38.exe.Config
  Directory:     c:\users\odinserj\documents\visual%20studio%202015\Projects\ConsoleApplication38\ConsoleApplication38\bin\Debug\

Following threads found:

Thread #1
  OS Thread ID:      10400
  AppDomain Address: 5029808
  Type:              Foreground

  Managed stack trace:
   - [InlinedCallFrame] at UNKNOWN
   - DomainNeutralILStubClass.IL_STUB_PInvoke(Microsoft.Win32.SafeHandles.SafeFileHandle, Byte*, Int32, Int32 ByRef, IntPtr) at mscorlib
   - [InlinedCallFrame] at UNKNOWN
   - System.IO.__ConsoleStream.ReadFileNative(Microsoft.Win32.SafeHandles.SafeFileHandle, Byte[], Int32, Int32, Boolean, Boolean, Int32 ByRef) at mscorlib
   - System.IO.__ConsoleStream.Read(Byte[], Int32, Int32) at mscorlib
   - System.IO.StreamReader.ReadBuffer() at mscorlib
   - System.IO.StreamReader.ReadLine() at mscorlib
   - System.IO.TextReader+SyncTextReader.ReadLine() at mscorlib
   - System.Console.ReadLine() at mscorlib
   - Microsoft.PFE.Samples.Program.Main() at ConsoleApplication38
   - [GCFrame] at UNKNOWN

Thread #2
  OS Thread ID:      2168
  AppDomain Address: 5029808
  Type:              Background
  Role:              Finalizer

  Managed stack trace:
   - [DebuggerU2MCatchHandlerFrame] at UNKNOWN
```
