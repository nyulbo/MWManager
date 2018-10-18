using System;

namespace MessageBus.Consumer
{
    public interface IConsumerDispatcher : IDisposable
    {
        void QueueAction(Action action);
        void OnDisconnected();
    }
}