using System;

namespace MessageBus.SystemMessages
{
    public class UnscheduleMe
    {
        public string CancellationKey { get; set; }
    }
}