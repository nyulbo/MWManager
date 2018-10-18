using System;

namespace MessageBus.Consumer
{
    public interface IConsumer : IDisposable
    {
        IDisposable StartConsuming();
    }
}