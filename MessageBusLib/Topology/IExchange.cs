namespace MessageBus.Topology
{
    public interface IExchange : IBindable
    {
        string Name { get; }
    }
}