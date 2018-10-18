using System;

namespace MessageBus.Loggers
{
    /// <summary>
    /// noop logger
    /// </summary>
    public class NullLogger : ILogger
    {
        public void DebugWrite(string format, params object[] args)
        {
            
        }

        public void InfoWrite(string format, params object[] args)
        {
            
        }

        public void ErrorWrite(string format, params object[] args)
        {
            
        }

        public void ErrorWrite(Exception exception)
        {
            
        }
    }
}