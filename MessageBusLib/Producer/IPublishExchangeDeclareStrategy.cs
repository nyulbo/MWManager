using System;
using System.Threading.Tasks;
using MessageBus.Topology;

namespace MessageBus.Producer
{
    public interface IPublishExchangeDeclareStrategy
    {
        IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType);
        IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType);        
        Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, string exchangeName, string exchangeType);
        Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, Type messageType, string exchangeType);
    }
}