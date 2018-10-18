namespace MessageBus.Producer
{
    public interface IPersistentChannelFactory
    {
        IPersistentChannel CreatePersistentChannel(IPersistentConnection connection);
    }

    public class PersistentChannelFactory : IPersistentChannelFactory
    {
        private readonly ILogger logger;
        private readonly ConnectionConfiguration configuration;
        private readonly IEventBus eventBus;

        public PersistentChannelFactory(ILogger logger, ConnectionConfiguration configuration, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.logger = logger;
            this.configuration = configuration;
            this.eventBus = eventBus;
        }

        public IPersistentChannel CreatePersistentChannel(IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");

            return new PersistentChannel(connection, logger, configuration, eventBus);
        }
    }
}