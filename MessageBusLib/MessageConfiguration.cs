namespace MessageBus
{
    public class MessageConfiguration 
    {
        public string ExchangeName { get; set; }
        public string QueueName { get; set; }
        public string RoutingKey { get; set; }
        public string ReplyQueueName { get; set; }
        public ushort RequestTimeOut { get; set; }
        public ushort PrefetchCount { get; set; }
        public MessageConfiguration()
        {
            RequestTimeOut = 10; // seconds
            PrefetchCount = 0;
        }
    }
}