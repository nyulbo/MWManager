using System;

namespace MessageBus.Consumer
{
    public interface IConsumerDispatcherFactory : IDisposable
    {
        IConsumerDispatcher GetConsumerDispatcher();
        void OnDisconnected();
    }
}