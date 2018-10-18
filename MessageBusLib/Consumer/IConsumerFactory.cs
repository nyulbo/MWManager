using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MessageBus.Topology;

namespace MessageBus.Consumer
{

    public interface IConsumerFactory : IDisposable
    {
        IConsumer CreateConsumer(
            ICollection<Tuple<IQueue, Func<Byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs,
            IPersistentConnection connection,
            IConsumerConfiguration configuration);

        IConsumer CreateConsumer(
            IQueue queue, 
            Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection,
            IConsumerConfiguration configuration
            );
    }
}