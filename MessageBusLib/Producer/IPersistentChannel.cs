using System;
using RabbitMQ.Client;

namespace MessageBus.Producer
{
    public interface IPersistentChannel : IDisposable
    {
        void InvokeChannelAction(Action<IModel> channelAction);
    }
}