using System;
using RabbitMQ.Client;

namespace MessageBus.Producer
{
    public interface IPublishConfirmationListener : IDisposable
    {
        IPublishConfirmationWaiter GetWaiter(IModel model);
    }
}