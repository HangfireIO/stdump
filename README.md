# stdump

Explore stack trace of a running managed process without downtime, or from a minidump file 

## Installation

You can install `stdump` using one of the following ways:

* **Manual**: Obtain an archive from the [latest release](https://github.com/odinserj/stdump/releases/latest).
* **NuGet**: `Install-Package stdump`
* **Chocolatey**: `choco install stdump`
 
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
