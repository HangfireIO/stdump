namespace STDump
{
    public enum ExitCode
    {
        Success = 0,
        UsageRequested = -1,
        Canceled = -2,
        TargetNotFound = -3,
        DiagnosticFailed = -4
    }
}