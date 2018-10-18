using MessageBus.Topology;

namespace MessageBus.Consumer
{
    public class HandlerCollectionFactory : IHandlerCollectionFactory
    {
        private readonly ILogger logger;

        public HandlerCollectionFactory(ILogger logger)
        {
            this.logger = logger;
        }

        public IHandlerCollection CreateHandlerCollection(IQueue queue)
        {
            return new HandlerCollection(logger);
        }
    }
}