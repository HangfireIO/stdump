using System;

namespace STDump
{
    public class ProcessNotFoundException : Exception
    {
        public ProcessNotFoundException(string message) : base(message)
        {
        }
    }
}