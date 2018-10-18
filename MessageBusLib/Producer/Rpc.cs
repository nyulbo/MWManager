﻿using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Events;
using MessageBus.Topology;
using MessageBus.FluentConfiguration;

namespace MessageBus.Producer
{
    /// <summary>
    /// Default implementation of EasyNetQ's request-response pattern
    /// </summary>
    public class Rpc : IRpc
    {
        private readonly ConnectionConfiguration connectionConfiguration;
        protected readonly IAdvancedBus advancedBus;
        protected readonly IConventions conventions;
        protected readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        protected readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly ITimeoutStrategy timeoutStrategy;
        private readonly ITypeNameSerializer typeNameSerializer;

        private readonly object responseQueuesAddLock = new object();
        private readonly ConcurrentDictionary<RpcKey, string> responseQueues = new ConcurrentDictionary<RpcKey, string>();
        private readonly ConcurrentDictionary<string, ResponseAction> responseActions = new ConcurrentDictionary<string, ResponseAction>();

        protected readonly TimeSpan disablePeriodicSignaling = TimeSpan.FromMilliseconds(-1);

        protected const string isFaultedKey = "IsFaulted";
        protected const string exceptionMessageKey = "ExceptionMessage";

        public Rpc(
            ConnectionConfiguration connectionConfiguration,
            IAdvancedBus advancedBus,
            IEventBus eventBus,
            IConventions conventions,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            ITimeoutStrategy timeoutStrategy,
            ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "configuration");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(timeoutStrategy, "timeoutStrategy");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.connectionConfiguration = connectionConfiguration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.timeoutStrategy = timeoutStrategy;
            this.typeNameSerializer = typeNameSerializer;

            eventBus.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated);
        }

        private void OnConnectionCreated(ConnectionCreatedEvent @event)
        {
            var copyOfResponseActions = responseActions.Values;
            responseActions.Clear();
            responseQueues.Clear();

            // retry in-flight requests.
            foreach (var responseAction in copyOfResponseActions)
            {
                responseAction.OnFailure();
            }
        }        
        public virtual Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();

            var tcs = new TaskCompletionSource<TResponse>();

            var requestType = typeof(TRequest);

            Timer timer = null;
#if !NETFX
            timer = new Timer(state =>
                {
                    timer.Dispose();
                    tcs.TrySetException(new TimeoutException(string.Format("Request timed out. CorrelationId: {0}", correlationId.ToString())));
                }, null, TimeSpan.FromSeconds(timeoutStrategy.GetTimeoutSeconds(requestType)), disablePeriodicSignaling);
#else
            timer = new Timer(state =>
            {
                ((Timer)state).Dispose();
                tcs.TrySetException(new TimeoutException(string.Format("Request timed out. CorrelationId: {0}", correlationId.ToString())));
            });

             timer.Change(TimeSpan.FromSeconds(timeoutStrategy.GetTimeoutSeconds(requestType)), disablePeriodicSignaling);
#endif

            RegisterErrorHandling(correlationId, timer, tcs);

            var queueName = SubscribeToResponse<TRequest, TResponse>();
            var routingKey = conventions.RpcRoutingKeyNamingConvention(requestType);
            RequestPublish(request, routingKey, queueName, correlationId);

            return tcs.Task;
        }

        public virtual Task<byte[]> Request(byte[] request, MessageConfiguration messageConfig)
        {
            Preconditions.CheckNotNull(request, "request");
            var exchange = new Exchange(messageConfig.ExchangeName);
            var correlationId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<byte[]>();
            var requestType = typeof(byte[]);
            Timer timer = null;
#if !NETFX
            timer = new Timer(state =>
            {
                timer.Dispose();
                tcs.TrySetException(new TimeoutException(string.Format("Request timed out. CorrelationId: {0}", correlationId.ToString())));
            }, null, TimeSpan.FromSeconds(messageConfig.RequestTimeOut), disablePeriodicSignaling);
#else
            timer = new Timer(state =>
            {
                ((Timer)state).Dispose();
                tcs.TrySetException(new TimeoutException(string.Format("Request timed out. CorrelationId: {0}", correlationId.ToString())));
            });

             timer.Change(TimeSpan.FromSeconds(timeoutStrategy.GetTimeoutSeconds(requestType)), disablePeriodicSignaling);
#endif     

            RegisterErrorHandling(correlationId, timer, tcs);
            
            var queueName = SubscribeToResponse();
            RequestPublish(request, exchange, messageConfig.RoutingKey, queueName, correlationId);

            return tcs.Task;
        }
        protected virtual string SubscribeToResponse()
        {
            var responseType = typeof(byte[]);
            var rpcKey = new RpcKey { Request = typeof(byte[]), Response = responseType };
            string queueName;
            if (responseQueues.TryGetValue(rpcKey, out queueName))
                return queueName;
            lock (responseQueuesAddLock)
            {
                if (responseQueues.TryGetValue(rpcKey, out queueName))
                    return queueName;

                var queue = advancedBus.QueueDeclare(
                            conventions.RpcReturnQueueNamingConvention(),
                            passive: false,
                            durable: false,
                            exclusive: true,
                            autoDelete: true);

                advancedBus.Consume(queue, (message, messageProperties, messageReceivedInfo) => Task.Factory.StartNew(() =>
                {
                    ResponseAction responseAction;
                    if (responseActions.TryRemove(messageProperties.CorrelationId, out responseAction))
                    {
                        responseAction.OnSuccessByte(message, messageProperties);
                    }
                }));
                responseQueues.TryAdd(rpcKey, queue.Name);
                return queue.Name;
            }
        }
        protected void RegisterErrorHandling(Guid correlationId, Timer timer, TaskCompletionSource<byte[]> tcs)
        {
            responseActions.TryAdd(correlationId.ToString(), new ResponseAction
            {
                OnSuccessByte = (message, properties) =>
                {
                    timer.Dispose();

                    bool isFaulted = false;
                    string exceptionMessage = "The exception message has not been specified.";
                    if (properties.HeadersPresent)
                    {
                        if (properties.Headers.ContainsKey(isFaultedKey))
                        {
                            isFaulted = Convert.ToBoolean(properties.Headers[isFaultedKey]);
                        }
                        if (properties.Headers.ContainsKey(exceptionMessageKey))
                        {
                            exceptionMessage = Encoding.UTF8.GetString((byte[])properties.Headers[exceptionMessageKey]);
                        }
                    }

                    if (isFaulted)
                    {
                        tcs.TrySetException(new MessageBusResponderException(exceptionMessage));
                    }
                    else
                    {
                        tcs.TrySetResult(message);
                    }
                },
                OnFailure = () =>
                {
                    timer.Dispose();
                    tcs.TrySetException(new MessageBusException("Connection lost while request was in-flight. CorrelationId: {0}", correlationId.ToString()));
                }
            });
        }
        protected virtual void RequestPublish(byte[] message, IExchange exchange, string routingKey, string returnQueueName, Guid correlationId)
        {
            var requestType = typeof(byte[]);

            var messageProperties = new MessageProperties {
                ReplyTo = returnQueueName,
                CorrelationId = correlationId.ToString(),
                Expiration = (timeoutStrategy.GetTimeoutSeconds(requestType) * 1000).ToString(),
                DeliveryMode = MessageDeliveryMode.NonPersistent
            };
            advancedBus.Publish(exchange, routingKey, false, messageProperties, message);
        }


        protected void RegisterErrorHandling<TResponse>(Guid correlationId, Timer timer, TaskCompletionSource<TResponse> tcs)
            where TResponse : class
        {
            responseActions.TryAdd(correlationId.ToString(), new ResponseAction
            {
                OnSuccess = message =>
                {
                    timer.Dispose();

                    var msg = ((IMessage<TResponse>)message);

                    bool isFaulted = false;
                    string exceptionMessage = "The exception message has not been specified.";
                    if(msg.Properties.HeadersPresent)
                    {
                        if(msg.Properties.Headers.ContainsKey(isFaultedKey))
                        {
                            isFaulted = Convert.ToBoolean(msg.Properties.Headers[isFaultedKey]);
                        }
                        if(msg.Properties.Headers.ContainsKey(exceptionMessageKey))
                        {
                            exceptionMessage = Encoding.UTF8.GetString((byte[])msg.Properties.Headers[exceptionMessageKey]);
                        }
                    }

                    if(isFaulted)
                    {
                        tcs.TrySetException(new MessageBusResponderException(exceptionMessage));
                    }
                    else
                    {
                        tcs.TrySetResult(msg.Body);
                    }
                },
                OnFailure = () =>
                {
                    timer.Dispose();
                    tcs.TrySetException(new MessageBusException("Connection lost while request was in-flight. CorrelationId: {0}", correlationId.ToString()));
                }
            });
        }

        protected virtual string SubscribeToResponse<TRequest, TResponse>()
            where TResponse : class
        {
            var responseType = typeof(TResponse);
            var rpcKey = new RpcKey { Request = typeof(TRequest), Response = responseType };
            string queueName;
            if (responseQueues.TryGetValue(rpcKey, out queueName))
                return queueName;
            lock (responseQueuesAddLock)
            {
                if (responseQueues.TryGetValue(rpcKey, out queueName))
                    return queueName;

                var queue = advancedBus.QueueDeclare(
                            conventions.RpcReturnQueueNamingConvention(),
                            passive: false,
                            durable: false,
                            exclusive: true,
                            autoDelete: true);

                var exchange = DeclareRpcExchange(conventions.RpcResponseExchangeNamingConvention(responseType));
                advancedBus.Bind(exchange, queue, queue.Name);                

                advancedBus.Consume<TResponse>(queue, (message, messageReceivedInfo) => Task.Factory.StartNew(() =>
                    {
                        ResponseAction responseAction;
                        if(responseActions.TryRemove(message.Properties.CorrelationId, out responseAction))
                        {
                            responseAction.OnSuccess(message);
                        }
                    }));
                responseQueues.TryAdd(rpcKey, queue.Name);
                return queue.Name;
            }
        }

        protected struct RpcKey
        {
            public Type Request;
            public Type Response;
        }

        protected class ResponseAction
        {
            public Action<object> OnSuccess { get; set; }
            public Action OnFailure { get; set; }
            public Action<byte[], MessageProperties> OnSuccessByte { get; set; }
        }
        protected virtual void RequestPublish<TRequest>(TRequest request, string routingKey, string replyQueueName, Guid correlationId)
            where TRequest : class
        {
            var requestType = typeof(TRequest);
            var exchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, 
                conventions.RpcRequestExchangeNamingConvention(requestType), ExchangeType.Direct);

            var requestMessage = new Message<TRequest>(request)
            {
                Properties =
                {
                    ReplyTo = replyQueueName,
                    CorrelationId = correlationId.ToString(),
                    Expiration = (timeoutStrategy.GetTimeoutSeconds(requestType) * 1000).ToString(),
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(requestType)
                }
            };

            advancedBus.Publish(exchange, routingKey, false, requestMessage);
        }
        public virtual IDisposable Respond(Func<byte[], Task<byte[]>> responder, Action<IResponderConfiguration> configure, string queueName)
        {
            Preconditions.CheckNotNull(responder, "responder");
            Preconditions.CheckNotNull(configure, "configure");
            // We're explicitely validating TResponse here because the type won't be used directly.
            // It'll only be used when executing a successful responder, which will silently fail if TResponse serialized length exceeds the limit.
            Preconditions.CheckShortString(typeNameSerializer.Serialize(typeof(byte[])), "byte[]");

            //var requestType = typeof(byte[]);

            var configuration = new ResponderConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration); 

            //var queue = advancedBus.QueueDeclare(queueName);
            var queue = new Queue(queueName, false);
            
            return advancedBus.Consume(queue
                , (message, requestMessage, messageReceivedInfo) => ExecuteResponder(responder, message, requestMessage, messageReceivedInfo)
                , c => c.WithPrefetchCount(configuration.PrefetchCount)
            );
        }
        protected Task ExecuteResponder(Func<byte[], Task<byte[]>> responder, byte[] message, MessageProperties properties, MessageReceivedInfo receiveInfo)
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                responder(message).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        if (task.Exception != null)
                        {
                            OnResponderFailure(message, properties, task.Exception.InnerException.Message);
                            tcs.SetException(task.Exception);
                        }
                    }
                    else
                    {
                        OnResponderSuccess(properties, task.Result);
                        tcs.SetResult(null);
                    }
                });
            }
            catch (Exception e)
            {
                OnResponderFailure(message, properties, e.Message);
                tcs.SetException(e);
            }

            return tcs.Task;
        }
        protected virtual void OnResponderFailure(byte[] message, MessageProperties properties, string exceptionMessage)
        {   
            properties.Headers.Add(isFaultedKey, true);
            properties.Headers.Add(exceptionMessageKey, exceptionMessage);
            properties.DeliveryMode = MessageDeliveryMode.NonPersistent;
            advancedBus.Publish(Exchange.GetDefault(), properties.ReplyTo, false, properties, message);
        }
        protected virtual void OnResponderSuccess(MessageProperties properties, byte[] responseMessage)
        {   
            var messageProperties = new MessageProperties
            {
                CorrelationId = properties.CorrelationId,
                DeliveryMode = MessageDeliveryMode.NonPersistent
            };
            advancedBus.Publish(Exchange.GetDefault(), properties.ReplyTo, false, messageProperties, responseMessage);
        }
        public virtual IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            return Respond(responder, c => { });
        }
        public virtual IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure) where TRequest : class where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");
            Preconditions.CheckNotNull(configure, "configure");
            // We're explicitely validating TResponse here because the type won't be used directly.
            // It'll only be used when executing a successful responder, which will silently fail if TResponse serialized length exceeds the limit.
            Preconditions.CheckShortString(typeNameSerializer.Serialize(typeof(TResponse)), "TResponse");

            var requestType = typeof(TRequest);

            var configuration = new ResponderConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration);

            var routingKey = conventions.RpcRoutingKeyNamingConvention(requestType);            
            var exchange = advancedBus.ExchangeDeclare(conventions.RpcRequestExchangeNamingConvention(requestType), ExchangeType.Direct);            
            var queue = advancedBus.QueueDeclare(routingKey);
            
            advancedBus.Bind(exchange, queue, routingKey);

            return advancedBus.Consume<TRequest>(queue, (requestMessage, messageReceivedInfo) => ExecuteResponder(responder, requestMessage),
                c => c.WithPrefetchCount(configuration.PrefetchCount));
        }
        protected Task ExecuteResponder<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IMessage<TRequest> requestMessage)
            where TRequest : class
            where TResponse : class
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                responder(requestMessage.Body).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        if (task.Exception != null)
                        {
                            OnResponderFailure<TRequest, TResponse>(requestMessage, task.Exception.InnerException.Message, task.Exception);
                            tcs.SetException(task.Exception);
                        }
                    }
                    else
                    {
                        OnResponderSuccess(requestMessage, task.Result);
                        tcs.SetResult(null);
                    }
                });
            }
            catch (Exception e)
            {
                OnResponderFailure<TRequest, TResponse>(requestMessage, e.Message, e);
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        protected virtual void OnResponderSuccess<TRequest, TResponse>(IMessage<TRequest> requestMessage, TResponse response)
            where TRequest : class
            where TResponse : class
        {
            var responseMessage = new Message<TResponse>(response)
            {
                Properties =
                {
                    CorrelationId = requestMessage.Properties.CorrelationId,
                    DeliveryMode = MessageDeliveryMode.NonPersistent
                }
            };

            //var exchange = DeclareRpcExchange(conventions.RpcResponseExchangeNamingConvention(typeof(TResponse)));

            advancedBus.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, false, responseMessage);
        }

        protected virtual void OnResponderFailure<TRequest, TResponse>(IMessage<TRequest> requestMessage, string exceptionMessage, Exception exception)
            where TRequest : class
            where TResponse : class
        {
            var body = ReflectionHelpers.CreateInstance<TResponse>();
            var responseMessage = new Message<TResponse>(body);
            responseMessage.Properties.Headers.Add(isFaultedKey, true);
            responseMessage.Properties.Headers.Add(exceptionMessageKey, exceptionMessage);
            responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;
            responseMessage.Properties.DeliveryMode = MessageDeliveryMode.NonPersistent;

            //var exchange = DeclareRpcExchange(conventions.RpcResponseExchangeNamingConvention(typeof(TResponse)));

            advancedBus.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, false, responseMessage);
        }

        private IExchange DeclareRpcExchange(string exchangeName)
        {
            return publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Direct);
        }
    }
}