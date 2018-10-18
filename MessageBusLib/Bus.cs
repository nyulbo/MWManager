using System;
using System.Text;
using System.Threading.Tasks;
using MessageBus.Consumer;
using MessageBus.FluentConfiguration;
using MessageBus.Producer;
using MessageBus.Topology;
using System.Linq;
using MessageBus.Internals;

namespace MessageBus
{
    public class Bus : IBus
    {
        private readonly IConventions conventions;
        private readonly IAdvancedBus advancedBus;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IRpc rpc;
        private readonly ISendReceive sendReceive;
        private readonly ConnectionConfiguration connectionConfiguration;

        public Bus(
            IConventions conventions,
            IAdvancedBus advancedBus,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IRpc rpc,
            ISendReceive sendReceive,
            ConnectionConfiguration connectionConfiguration)
        {
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(rpc, "rpc");
            Preconditions.CheckNotNull(sendReceive, "sendReceive");
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");

            this.conventions = conventions;
            this.advancedBus = advancedBus;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.rpc = rpc;
            this.sendReceive = sendReceive;
            this.connectionConfiguration = connectionConfiguration;
        }
        /// <summary>
        /// 결합 
        /// </summary>
        /// <param name="exchangeName">Exchagen 명</param>
        /// <param name="routingKey">Routing Key</param>
        /// <param name="queueName">Queue 명</param>
        public void Bind(string exchangeName, string routingKey, string queueName)
        {
            IExchange exchange = new Exchange(exchangeName);
            IQueue queue = new Queue(queueName, false);
            var bind = advancedBus.Bind(exchange, queue, routingKey);
        }
        /// <summary>
        /// 결합 해제
        /// </summary>
        /// <param name="exchangeName">Exchage 명</param>
        /// <param name="routingKey">Routing Key</param>
        /// <param name="queueName">Queue 명</param>
        public void Unbind(string exchangeName, string routingKey, string queueName)
        {
            IExchange exchange = new Exchange(exchangeName);
            IBindable queue = new Queue(queueName, false);
            IBinding binding = new Binding(queue, exchange, routingKey);
            advancedBus.BindingDelete(binding);
        }
        /// <summary>
        /// Queue 삭제
        /// </summary>
        /// <param name="queueName">Queue 명</param>
        public void DeleteQueue(string queueName) 
        {
            IQueue queue = new Queue(queueName, false);
            advancedBus.QueueDelete(queue);
        }
        /// <summary>
        /// Queue 선언
        /// </summary>
        /// <param name="queueName">Queue 명</param>
        public void DeclareQueue(string queueName)
        {
            var queue = advancedBus.QueueDeclare(queueName);
        }
        /// <summary>
        /// Queue 메세지 제거
        /// </summary>
        /// <param name="queueName">Queue Name</param>
        public void PurgeQueue(string queueName)
        {
            var queue = new Queue(queueName, false);
            advancedBus.QueuePurge(queue);
        }
        /// <summary>
        /// 메세시 게시
        /// </summary>
        /// <param name="messageConfig">메세지 게시 설정</param>
        /// <param name="message">게시할 메세지</param>
        public void Publish(MessageConfiguration messageConfig, byte[] message)
        {
            var exchange = new Exchange(messageConfig.ExchangeName);
            advancedBus.Publish(exchange, 
                messageConfig.RoutingKey, 
                false,
                new MessageProperties()
                {
                    DeliveryMode = MessageDeliveryMode.Persistent
                },
                message);
        }
        /// <summary>
        /// 메세지 구독
        /// </summary>
        /// <param name="onMessage">메세지 도착시 실행될 Func</param>
        /// <param name="messageConfig">메세지 구독 설정</param>
        public IDisposable Subscribe(Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, MessageConfiguration messageConfig)
        {
            var queue = new Queue(messageConfig.QueueName, false);
            return advancedBus.Consume(queue, onMessage, config => config.WithPrefetchCount(messageConfig.PrefetchCount));
            //return advancedBus.Consume(queue, onMessage);
        }
        /// <summary>
        /// 메세지 가져오기
        /// </summary>
        /// <param name="queueName">메세지 큐 이름</param>
        /// <returns>IBasicGetResult</returns>
        public IBasicGetResult Get(string queueName)
        {
            var queue = new Queue(queueName, false);
            return advancedBus.Get(queue);
        }
        /// <summary>
        /// 메세지 요청 
        /// </summary>
        /// <param name="request">요청 메세지</param>
        /// <param name="messageConfig">메세지 요청 설정</param>
        /// <returns>응답 메세지</returns>
        public byte[] Request(byte[] request, MessageConfiguration messageConfig)
        {
            Preconditions.CheckNotNull(request, "request");

            var task = rpc.Request(request, messageConfig);
            task.Wait();
            return task.Result;
        }
        /// <summary>
        /// 메세지 응답
        /// </summary>
        /// <param name="responder">메세지 응답시 실행될 Func</param>
        /// <param name="queueName">메세지 큐 이름</param>
        public IDisposable Reply(Func<byte[], byte[]> responder, string queueName)
        {
            Preconditions.CheckNotNull(responder, "responder");
            Func<byte[], Task<byte[]>> taskResponder =
                request => Task<byte[]>.Factory.StartNew(_ => responder(request), null);
            
            return rpc.Respond(taskResponder, configure => { }, queueName);
        }
        public virtual void Dispose()
        {
            advancedBus.Dispose();
        }

        #region -- 미사용
        //public void Publish(MessageProperties properties, byte[] message)
        //{
        //    var messageProperties = new MessageProperties
        //    {
        //        CorrelationId = properties.CorrelationId,
        //        DeliveryMode = MessageDeliveryMode.NonPersistent
        //    };
        //    advancedBus.Publish(Exchange.GetDefault(), properties.ReplyTo, false, messageProperties, message);
        //}
        //public void Publish<T>(T message) where T : class
        //{
        //    Preconditions.CheckNotNull(message, "message");

        //    Publish(message, conventions.TopicNamingConvention(typeof(T)));
        //}

        //public void Publish<T>(T message, string topic) where T : class
        //{
        //    Preconditions.CheckNotNull(message, "message");
        //    Preconditions.CheckNotNull(topic, "topic");

        //    Publish(message, c => c.WithTopic(topic));
        //}

        //public void Publish<T>(T message, Action<IPublishConfiguration> configure) where T : class
        //{
        //    Preconditions.CheckNotNull(message, "message");
        //    Preconditions.CheckNotNull(configure, "configure");

        //    var configuration = new PublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
        //    configure(configuration);

        //    var messageType = typeof(T);
        //    var busMessage = new Message<T>(message)
        //    {
        //        Properties =
        //        {
        //            DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType)
        //        }
        //    };
        //    if (configuration.Priority != null)
        //        busMessage.Properties.Priority = configuration.Priority.Value;
        //    if (configuration.Expires != null)
        //        busMessage.Properties.Expiration = configuration.Expires.ToString();

        //    var exchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, messageType, ExchangeType.Topic);
        //    advancedBus.Publish(exchange, configuration.Topic, false, busMessage);
        //}

        //public Task PublishAsync<T>(T message) where T : class
        //{
        //    Preconditions.CheckNotNull(message, "message");

        //    return PublishAsync(message, conventions.TopicNamingConvention(typeof(T)));
        //}

        //public Task PublishAsync<T>(T message, string topic) where T : class
        //{
        //    Preconditions.CheckNotNull(message, "message");
        //    Preconditions.CheckNotNull(topic, "topic");

        //    return PublishAsync(message, c => c.WithTopic(topic));
        //}

        //public async Task PublishAsync<T>(T message, Action<IPublishConfiguration> configure) where T : class
        //{
        //    Preconditions.CheckNotNull(message, "message");
        //    Preconditions.CheckNotNull(configure, "configure");

        //    var configuration = new PublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
        //    configure(configuration);

        //    var messageType = typeof(T);
        //    var busMessage = new Message<T>(message)
        //    {
        //        Properties =
        //        {
        //            DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType)
        //        }
        //    };
        //    if (configuration.Priority != null)
        //        busMessage.Properties.Priority = configuration.Priority.Value;
        //    if (configuration.Expires != null)
        //        busMessage.Properties.Expiration = configuration.Expires.ToString();

        //    var exchange = await publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, messageType, ExchangeType.Topic).ConfigureAwait(false);
        //    await advancedBus.PublishAsync(exchange, configuration.Topic, false, busMessage).ConfigureAwait(false);
        //}

        //public ISubscriptionResult Subscribe<T>(string subscriptionId, Action<T> onMessage) where T : class
        //{
        //    return Subscribe(subscriptionId, onMessage, x => { });
        //}

        //public ISubscriptionResult Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
        //{
        //    Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
        //    Preconditions.CheckNotNull(onMessage, "onMessage");
        //    Preconditions.CheckNotNull(configure, "configure");

        //    return SubscribeAsync<T>(subscriptionId, msg => TaskHelpers.ExecuteSynchronously(() => onMessage(msg)), configure);
        //}

        //public ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage) where T : class
        //{
        //    return SubscribeAsync(subscriptionId, onMessage, x => { });
        //}

        //public ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
        //{
        //    Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
        //    Preconditions.CheckNotNull(onMessage, "onMessage");
        //    Preconditions.CheckNotNull(configure, "configure");

        //    var configuration = new SubscriptionConfiguration(connectionConfiguration.PrefetchCount);
        //    configure(configuration);

        //    var queueName = conventions.QueueNamingConvention(typeof(T), subscriptionId);
        //    var exchangeName = conventions.ExchangeNamingConvention(typeof(T));

        //    var queue = advancedBus.QueueDeclare(queueName, autoDelete: configuration.AutoDelete, durable: configuration.Durable, expires: configuration.Expires, maxPriority: configuration.MaxPriority);
        //    var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);

        //    foreach (var topic in configuration.Topics.DefaultIfEmpty("#"))
        //    {
        //        advancedBus.Bind(exchange, queue, topic);
        //    }
        //    var consumerCancellation = advancedBus.Consume<T>(
        //        queue,
        //        (message, messageReceivedInfo) => onMessage(message.Body),
        //        x =>
        //        {
        //            x.WithPriority(configuration.Priority)
        //             .WithCancelOnHaFailover(configuration.CancelOnHaFailover)
        //             .WithPrefetchCount(configuration.PrefetchCount);
        //            if (configuration.IsExclusive)
        //            {
        //                x.AsExclusive();
        //            }
        //        });
        //    return new SubscriptionResult(exchange, queue, consumerCancellation);
        //}

        //public TResponse Request<TRequest, TResponse>(TRequest request)
        //    where TRequest : class
        //    where TResponse : class
        //{
        //    Preconditions.CheckNotNull(request, "request");

        //    var task = RequestAsync<TRequest, TResponse>(request);
        //    task.Wait();
        //    return task.Result;
        //}
        //public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
        //    where TRequest : class
        //    where TResponse : class
        //{
        //    Preconditions.CheckNotNull(request, "request");

        //    return rpc.Request<TRequest, TResponse>(request);
        //}
        //public IDisposable Reply(string queueName, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        //{
        //    var queue = new Queue(queueName, false);
        //    return advancedBus.Consume(queue, onMessage);
        //}
        //public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        //    where TRequest : class
        //    where TResponse : class
        //{
        //    Preconditions.CheckNotNull(responder, "responder");

        //    Func<TRequest, Task<TResponse>> taskResponder =
        //        request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

        //    return RespondAsync(taskResponder);
        //}
        //public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder, Action<IResponderConfiguration> configure) where TRequest : class where TResponse : class
        //{
        //    Func<TRequest, Task<TResponse>> taskResponder =
        //        request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

        //    return RespondAsync(taskResponder, configure);
        //}

        //public IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        //    where TRequest : class
        //    where TResponse : class
        //{
        //    return RespondAsync(responder, c => { });
        //}

        //public IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure) where TRequest : class where TResponse : class
        //{
        //    Preconditions.CheckNotNull(responder, "responder");
        //    Preconditions.CheckNotNull(configure, "configure");

        //    return rpc.Respond(responder, configure);
        //}
        //public void Send<T>(string queue, T message)
        //    where T : class
        //{
        //    sendReceive.Send(queue, message);
        //}

        //public Task SendAsync<T>(string queue, T message)
        //    where T : class
        //{
        //    return sendReceive.SendAsync(queue, message);
        //}

        //public IDisposable Receive<T>(string queue, Action<T> onMessage)
        //    where T : class
        //{
        //    return sendReceive.Receive(queue, onMessage);
        //}

        //public IDisposable Receive<T>(string queue, Action<T> onMessage, Action<IConsumerConfiguration> configure)
        //    where T : class
        //{
        //    return sendReceive.Receive(queue, onMessage, configure);
        //}

        //public IDisposable Receive<T>(string queue, Func<T, Task> onMessage)
        //    where T : class
        //{
        //    return sendReceive.Receive(queue, onMessage);
        //}

        //public IDisposable Receive<T>(string queue, Func<T, Task> onMessage, Action<IConsumerConfiguration> configure)
        //    where T : class
        //{
        //    return sendReceive.Receive(queue, onMessage, configure);
        //}

        //public IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers)
        //{
        //    return sendReceive.Receive(queue, addHandlers);
        //}

        //public IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers, Action<IConsumerConfiguration> configure)
        //{
        //    return sendReceive.Receive(queue, addHandlers, configure);
        //}

        //public bool IsConnected => advancedBus.IsConnected;

        //public IAdvancedBus Advanced => advancedBus;
        #endregion -- 미사용
    }
}