using System;

namespace MessageBus
{
    public interface ILogger
    {
        void DebugWrite(string format, params object[] args);
        void InfoWrite(string format, params object[] args);
        void ErrorWrite(string format, params object[] args);
        void ErrorWrite(Exception exception);

    }
}