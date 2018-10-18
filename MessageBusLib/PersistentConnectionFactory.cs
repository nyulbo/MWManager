using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBus
{
    public class PersistentConnectionFactory : IPersistentConnectionFactory
    {
        private readonly IEventBus eventBus;
        private readonly IConnectionFactory connectionFactory;
        private readonly ILogger logger;

        public PersistentConnectionFactory(ILogger logger, IConnectionFactory connectionFactory, IEventBus eventBus)
        {
            this.logger = logger;
            this.connectionFactory = connectionFactory;
            this.eventBus = eventBus;
        }

        public IPersistentConnection CreateConnection()
        {
            return new PersistentConnection(connectionFactory, logger, eventBus);
        }
    }
}
