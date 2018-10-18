namespace MessageBus
{   public interface IPersistentConnectionFactory
    {
        IPersistentConnection CreateConnection();
    }
}
