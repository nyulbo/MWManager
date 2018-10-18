using MessageBus.Topology;

namespace MessageBus.Consumer
{
    public interface IHandlerCollectionFactory
    {
        IHandlerCollection CreateHandlerCollection(IQueue queue);
    }
}