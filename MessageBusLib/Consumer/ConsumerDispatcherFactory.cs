using System;

namespace MessageBus.Consumer
{
    /// <summary>
    /// The default ConsumerDispatcherFactory. It creates a single dispatch
    /// queue which all consumers share.
    /// </summary>
    public class ConsumerDispatcherFactory : IConsumerDispatcherFactory
    {
        private readonly Lazy<IConsumerDispatcher> dispatcher;

        public ConsumerDispatcherFactory(ConnectionConfiguration configuration, ILogger logger)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(logger, "logger");
            
            dispatcher = new Lazy<IConsumerDispatcher>(() => new ConsumerDispatcher(configuration, logger));
        }

        public IConsumerDispatcher GetConsumerDispatcher()
        {
            return dispatcher.Value;
        }

        public void OnDisconnected()
        {
            if (dispatcher.IsValueCreated)
            {
                dispatcher.Value.OnDisconnected();
            }
        }

        public void Dispose()
        {
            if (dispatcher.IsValueCreated)
            {
                dispatcher.Value.Dispose();
            }
        }
    }
}