using System;


namespace MessageBus
{
	public delegate string ExchangeNameConvention(Type messageType);
	public delegate string TopicNameConvention(Type messageType);
    public delegate string QueueNameConvention(Type messageType, string subscriberId);
    public delegate string RpcRoutingKeyNamingConvention(Type messageType);

    public delegate string ErrorQueueNameConvention();
    public delegate string ErrorExchangeNameConvention(MessageReceivedInfo info);
    public delegate string RpcExchangeNameConvention(Type messageType);
    public delegate string RpcReturnQueueNamingConvention();
    
    public delegate string ConsumerTagConvention();

	public interface IConventions
	{
		ExchangeNameConvention ExchangeNamingConvention { get; set; }
		TopicNameConvention TopicNamingConvention { get; set; }
        QueueNameConvention QueueNamingConvention { get; set; }
        RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; set; }

        ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; }
        ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; }
        RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; set; }
        RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; set; }
        RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }

        ConsumerTagConvention ConsumerTagConvention { get; set; }
	}

	public class Conventions : IConventions
	{
		public Conventions(ITypeNameSerializer typeNameSerializer)
		{
		    Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

		    // Establish default conventions.
            ExchangeNamingConvention = messageType =>
            {
                var attr = GetQueueAttribute(messageType);

                return string.IsNullOrEmpty(attr.ExchangeName)
                    ? typeNameSerializer.Serialize(messageType)
                    : attr.ExchangeName;
            };
			
            TopicNamingConvention = messageType => "";
			
            QueueNamingConvention =
					(messageType, subscriptionId) =>
					{
                        var attr = GetQueueAttribute(messageType);

                        if (string.IsNullOrEmpty(attr.QueueName))
                        {
                            var typeName = typeNameSerializer.Serialize(messageType);

                            return string.IsNullOrEmpty(subscriptionId)
                                ? typeName
                                : string.Format("{0}_{1}", typeName, subscriptionId);
                        }

                        return string.IsNullOrEmpty(subscriptionId)
                            ? attr.QueueName
                            : string.Format("{0}_{1}", attr.QueueName, subscriptionId);
					};

            //RpcRoutingKeyNamingConvention = typeNameSerializer.Serialize;
            //RpcRequestExchangeNamingConvention = (type) => "easy_net_q_rpc";
            //RpcResponseExchangeNamingConvention = (type) => "easy_net_q_rpc";
            //RpcReturnQueueNamingConvention = () => "easynetq.response." + Guid.NewGuid();

            ErrorQueueNamingConvention = () => "default-error-queue";
		    ErrorExchangeNamingConvention = info => "error-exchagne-" + info.RoutingKey;

            //RpcRoutingKeyNamingConvention = typeNameSerializer.Serialize;
            //RpcRequestExchangeNamingConvention = (type) => "HMI.Control";
            //RpcResponseExchangeNamingConvention = (type) => "HMI.Control";
            //RpcReturnQueueNamingConvention = () => "Result.Queue." + Guid.NewGuid();

            RpcRoutingKeyNamingConvention = (type) => "rpc-routingkey";
            RpcRequestExchangeNamingConvention = (type) => "rpc-ex";
            RpcResponseExchangeNamingConvention = (type) => "rpc-ex";
            RpcReturnQueueNamingConvention = () => "reply-queue-" + Guid.NewGuid();

            ConsumerTagConvention = () => Guid.NewGuid().ToString();
		}

        private QueueAttribute GetQueueAttribute(Type messageType)
        {
            return messageType.GetAttribute<QueueAttribute>() ?? new QueueAttribute(string.Empty);
        }

		public ExchangeNameConvention ExchangeNamingConvention { get; set; }
		public TopicNameConvention TopicNamingConvention { get; set; }
        public QueueNameConvention QueueNamingConvention { get; set; }
        public RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; set; }

        public ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; }
        public ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; }
        public RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; set; }
        public RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; set; }
        public RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }

        public ConsumerTagConvention ConsumerTagConvention { get; set; }
	}
}