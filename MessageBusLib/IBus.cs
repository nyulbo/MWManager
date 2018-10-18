using System;
using System.Threading.Tasks;
using MessageBus.Consumer;
using MessageBus.FluentConfiguration;
using MessageBus.Producer;
using MessageBus.Topology;

namespace MessageBus
{
    /// <summary>
    /// Provides a simple Publish/Subscribe and Request/Response API for a message bus.
    /// </summary>
    public interface IBus : IDisposable
    {
        /// <summary>
        /// 결합 
        /// </summary>
        /// <param name="exchangeName">Exchagen 명</param>
        /// <param name="queueName">Queue 명</param>
        /// <param name="routingKey">Routing Key</param>
        void Bind(string exchangeName, string routingKey, string queueName);
        /// <summary>
        /// 결합 해제
        /// </summary>
        /// <param name="exchangeName">Exchage 명</param>
        /// <param name="queueName">Queue 명</param>
        /// <param name="routingKey">Routing Key</param>
        void Unbind(string exchangeName, string routingKey, string queueName);
        /// <summary>
        /// Queue 삭제
        /// </summary>
        /// <param name="queueName">Queue 명</param>
        void DeleteQueue(string queueName);
        /// <summary>
        /// Queue 선언
        /// </summary>
        /// <param name="queueName">Queue 명</param>
        void DeclareQueue(string queueName);
        /// <summary>
        /// Queue 메세지 제거
        /// </summary>
        /// <param name="queueName">Queue Name</param>
        void PurgeQueue(string queueName);
        /// <summary>
        /// 메세시 게시
        /// </summary>
        /// <param name="messageConfig">메세지 게시 설정</param>
        /// <param name="message">게시할 메세지</param>
        void Publish(MessageConfiguration messageConfig, byte[] message);
        /// <summary>
        /// 메세지 구독
        /// </summary>
        /// <param name="onMessage">메세지 도착시 실행될 Func</param>
        /// <param name="messageConfig">메세지 구독 설정</param>
        IDisposable Subscribe(Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, MessageConfiguration config);
        /// <summary>
        /// 메세지 가져오기
        /// </summary>
        /// <param name="queueName">메세지 큐 이름</param>
        /// <returns>IBasicGetResult</returns>
        IBasicGetResult Get(string queueName);
        /// <summary>
        /// 메세지 요청 
        /// </summary>
        /// <param name="request">요청 메세지</param>
        /// <param name="messageConfig">메세지 요청 설정</param>
        /// <returns>응답 메세지</returns>
        byte[] Request(byte[] request, MessageConfiguration messageConfig);
        /// <summary>
        /// 메세지 응답
        /// </summary>
        /// <param name="responder">메세지 응답시 실행될 Func</param>
        /// <param name="queueName">메세지 큐 이름</param>
        IDisposable Reply(Func<byte[], byte[]> responder, string queueName);

        #region -- 미사용
        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        //void Publish<T>(T message) where T : class;

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
        /// </param>
        //void Publish<T>(T message, Action<IPublishConfiguration> configure) where T : class;

        /// <summary>
        /// Publishes a message with a topic
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <param name="topic">The topic string</param>
        //void Publish<T>(T message, string topic) where T : class;

        /// <summary>
        /// Publishes a message.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <returns></returns>
        //Task PublishAsync<T>(T message) where T : class;

        /// <summary>
        /// Publishes a message.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
        /// </param>
        /// <returns></returns>
        //Task PublishAsync<T>(T message, Action<IPublishConfiguration> configure) where T : class;

        /// <summary>
        /// Publishes a message with a topic.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <param name="topic">The topic string</param>
        /// <returns></returns>
        //Task PublishAsync<T>(T message, string topic) where T : class;

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. When onMessage completes the message
        /// recipt is Ack'd. All onMessage delegates are processed on a single thread so you should
        /// avoid long running blocking IO operations. Consider using SubscribeAsync
        /// </param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        //ISubscriptionResult Subscribe<T>(string subscriptionId, Action<T> onMessage) where T : class;

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. When onMessage completes the message
        /// recipt is Ack'd. All onMessage delegates are processed on a single thread so you should
        /// avoid long running blocking IO operations. Consider using SubscribeAsync
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("uk.london")
        /// </param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        //ISubscriptionResult Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration> configure) 
        //    where T : class;

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// Allows the subscriber to complete asynchronously.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. onMessage can immediately return a Task and
        /// then continue processing asynchronously. When the Task completes the message will be
        /// Ack'd.
        /// </param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        //ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage) where T : class;

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. onMessage can immediately return a Task and
        /// then continue processing asynchronously. When the Task completes the message will be
        /// Ack'd.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("uk.london").WithArgument("x-message-ttl", "60")
        /// </param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        //ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration> configure) 
        //    where T : class;        

        /// <summary>
        /// Makes an RPC style request
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <returns>The response</returns>
        //TResponse Request<TRequest, TResponse>(TRequest request)
        //    where TRequest : class
        //    where TResponse : class;     

        /// <summary>
        /// Makes an RPC style request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <returns>A task that completes when the response returns</returns>
        //Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
        //    where TRequest : class
        //    where TResponse : class;

        /// <summary>
        /// Makes an RPC style request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <returns>A task that completes when the response returns</returns>
        //Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, IMessageConfiguration messageConfig)
        //    where TRequest : class
        //    where TResponse : class;

        /// <summary>
        /// Responds to an RPC request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="responder">
        /// A function to run when the request is received. It should return the response.
        /// </param>
        //IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder) 
        //    where TRequest : class
        //    where TResponse : class;

        /// <summary>
        /// Responds to an RPC request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="responder">
        /// A function to run when the request is received. It should return the response.
        /// </param>
        /// <param name="configure">
        /// A function for responder configuration
        /// </param>

        /// <summary>
        /// Responds to an RPC request asynchronously.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">
        /// A function to run when the request is received.
        /// </param>
        //IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder) 
        //    where TRequest : class
        //    where TResponse : class;

        /// <summary>
        /// Responds to an RPC request asynchronously.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">
        /// A function to run when the request is received.
        /// </param>
        /// <param name="configure">
        /// A function for responder configuration
        /// </param>
        //IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IMessageConfiguration messageConfig)
        //    where TRequest : class
        //    where TResponse : class;

        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        //void Send<T>(string queue, T message) where T : class;

        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        //Task SendAsync<T>(string queue, T message) where T : class;

        /// <summary>
        /// Receive messages from a queue.
        /// Multiple calls to Receive for the same queue, but with different message types
        /// will add multiple message handlers to the same consumer.
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The message handler</param>
        //IDisposable Receive<T>(string queue, Action<T> onMessage) where T : class;

        /// <summary>
        /// Receive messages from a queue.
        /// Multiple calls to Receive for the same queue, but with different message types
        /// will add multiple message handlers to the same consumer.
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The message handler</param>
        /// <param name="configure">Action to configure consumer with</param>
        //IDisposable Receive<T>(string queue, Action<T> onMessage, Action<IConsumerConfiguration> configure) where T : class;

        /// <summary>
        /// Receive messages from a queue.
        /// Multiple calls to Receive for the same queue, but with different message types
        /// will add multiple message handlers to the same consumer.
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asychronous message handler</param>
        //IDisposable Receive<T>(string queue, Func<T, Task> onMessage) where T : class;

        /// <summary>
        /// Receive messages from a queue.
        /// Multiple calls to Receive for the same queue, but with different message types
        /// will add multiple message handlers to the same consumer.
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asychronous message handler</param>
        /// <param name="configure">Action to configure consumer with</param>
        //IDisposable Receive<T>(string queue, Func<T, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class;

        /// <summary>
        /// Receive a message from the specified queue. Dispatch them to the given handlers
        /// </summary>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        //IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers);

        /// <summary>
        /// Receive a message from the specified queue. Dispatch them to the given handlers
        /// </summary>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        //IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers, Action<IConsumerConfiguration> configure);

        /// <summary>
        /// True if the bus is connected, False if it is not.
        /// </summary>
        //bool IsConnected { get; }

        /// <summary>
        /// Return the advanced EasyNetQ advanced API.
        /// </summary>
        //IAdvancedBus Advanced { get; }
        #endregion -- 미사용
    }
}