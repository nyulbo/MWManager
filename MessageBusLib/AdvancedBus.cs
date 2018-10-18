﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessageBus.Consumer;
using MessageBus.Events;
using MessageBus.Interception;
using MessageBus.Internals;
using MessageBus.Producer;
using MessageBus.Topology;
using RabbitMQ.Client.Events;

namespace MessageBus
{
    public class AdvancedBus : IAdvancedBus
    {
        private readonly IConsumerFactory consumerFactory;
        private readonly ILogger logger;
        private readonly IPublishConfirmationListener confirmationListener;
        private readonly IPersistentConnection connection;
        private readonly IClientCommandDispatcher clientCommandDispatcher;
        private readonly IEventBus eventBus;
        private readonly IHandlerCollectionFactory handlerCollectionFactory;
        private readonly ConnectionConfiguration connectionConfiguration;
        private readonly IProduceConsumeInterceptor produceConsumeInterceptor;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;

        public AdvancedBus(
            IConnectionFactory connectionFactory,
            IConsumerFactory consumerFactory,
            ILogger logger,
            IClientCommandDispatcherFactory clientCommandDispatcherFactory,
            IPublishConfirmationListener confirmationListener,
            IEventBus eventBus,
            IHandlerCollectionFactory handlerCollectionFactory,
            IContainer container,
            ConnectionConfiguration connectionConfiguration,
            IProduceConsumeInterceptor produceConsumeInterceptor,
            IMessageSerializationStrategy messageSerializationStrategy,
            IConventions conventions,
            AdvancedBusEventHandlers advancedBusEventHandlers,
            IPersistentConnectionFactory persistentConnectionFactory)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(consumerFactory, "consumerFactory");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(handlerCollectionFactory, "handlerCollectionFactory");
            Preconditions.CheckNotNull(container, "container");
            Preconditions.CheckNotNull(messageSerializationStrategy, "messageSerializationStrategy");
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(produceConsumeInterceptor, "produceConsumeInterceptor");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");
            Preconditions.CheckNotNull(persistentConnectionFactory, "persistentConnectionFactory");

            this.consumerFactory = consumerFactory;
            this.logger = logger;
            this.confirmationListener = confirmationListener;
            this.eventBus = eventBus;
            this.handlerCollectionFactory = handlerCollectionFactory;
            this.Container = container;
            this.connectionConfiguration = connectionConfiguration;
            this.produceConsumeInterceptor = produceConsumeInterceptor;
            this.messageSerializationStrategy = messageSerializationStrategy;
            this.Conventions = conventions;

            this.eventBus.Subscribe<ConnectionCreatedEvent>(e => OnConnected());
            if (advancedBusEventHandlers.Connected != null)
            {
                Connected += advancedBusEventHandlers.Connected;
            }
            this.eventBus.Subscribe<ConnectionDisconnectedEvent>(e => OnDisconnected());
            if (advancedBusEventHandlers.Disconnected != null)
            {
                Disconnected += advancedBusEventHandlers.Disconnected;
            }
            this.eventBus.Subscribe<ConnectionBlockedEvent>(OnBlocked);
            if (advancedBusEventHandlers.Blocked != null)
            {
                Blocked += advancedBusEventHandlers.Blocked;
            }
            this.eventBus.Subscribe<ConnectionUnblockedEvent>(e => OnUnblocked());
            if (advancedBusEventHandlers.Unblocked != null)
            {
                Unblocked += advancedBusEventHandlers.Unblocked;
            }
            this.eventBus.Subscribe<ReturnedMessageEvent>(OnMessageReturned);
            if (advancedBusEventHandlers.MessageReturned != null)
            {
                MessageReturned += advancedBusEventHandlers.MessageReturned;
            }

            connection = persistentConnectionFactory.CreateConnection();
            clientCommandDispatcher = clientCommandDispatcherFactory.GetClientCommandDispatcher(connection);
            connection.Initialize();
        }


        // ---------------------------------- consume --------------------------------------
        public IDisposable Consume(IEnumerable<QueueConsumerPair> queueConsumerPairs, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queueConsumerPairs, nameof(queueConsumerPairs));
            Preconditions.CheckNotNull(configure, "configure");

            if (disposed)
                throw new MessageBusException("This bus has been disposed");

            var queueOnMessages = queueConsumerPairs.Select(x =>
            {
                var onMessage = x.OnMessage;
                if (onMessage == null)
                {
                    var handlerCollection = handlerCollectionFactory.CreateHandlerCollection(x.Queue);
                    x.AddHandlers(handlerCollection);

                    onMessage = (body, properties, messageReceivedInfo) =>
                    {
                        var deserializedMessage = messageSerializationStrategy.DeserializeMessage(properties, body);
                        var handler = handlerCollection.GetHandler(deserializedMessage.MessageType);
                        return handler(deserializedMessage, messageReceivedInfo);
                    };
                }
                return Tuple.Create(x.Queue, onMessage);
            }).ToList();

            var consumerConfiguration = new ConsumerConfiguration(connectionConfiguration.PrefetchCount);
            configure(consumerConfiguration);
            var consumer = consumerFactory.CreateConsumer(queueOnMessages, connection, consumerConfiguration);

            return consumer.StartConsuming();
        }

        public IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage) where T : class
        {
            return Consume<T>(queue, onMessage, x => { });
        }

        public IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage, Action<IConsumerConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return Consume<T>(queue, (message, info) => TaskHelpers.ExecuteSynchronously(() => onMessage(message, info)), configure);
        }

        public virtual IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage)
            where T : class
        {
            return Consume(queue, onMessage, x => { });
        }

        public IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return Consume(queue, x => x.Add(onMessage), configure);
        }

        public virtual IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers)
        {
            return Consume(queue, addHandlers, x => { });
        }

        public IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(addHandlers, "addHandlers");
            Preconditions.CheckNotNull(configure, "configure");

            var handlerCollection = handlerCollectionFactory.CreateHandlerCollection(queue);
            addHandlers(handlerCollection);

            return Consume(queue, (body, properties, messageReceivedInfo) =>
            {
                var deserializedMessage = messageSerializationStrategy.DeserializeMessage(properties, body);
                var handler = handlerCollection.GetHandler(deserializedMessage.MessageType);
                return handler(deserializedMessage, messageReceivedInfo);

            }, configure);
        }

        public IDisposable Consume(IQueue queue, Action<byte[], MessageProperties, MessageReceivedInfo> onMessage)
        {
            return Consume(queue, (bytes, properties, info) => TaskHelpers.ExecuteSynchronously(() => onMessage(bytes, properties, info)));
        }

        public IDisposable Consume(IQueue queue, Action<byte[], MessageProperties, MessageReceivedInfo> onMessage, Action<IConsumerConfiguration> configure)
        {
            return Consume(queue, (bytes, properties, info) => TaskHelpers.ExecuteSynchronously(() => onMessage(bytes, properties, info)), configure);
        }

        public IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            return Consume(queue, onMessage, x => { });
        }

        public virtual IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            if (disposed)
                throw new MessageBusException("This bus has been disposed");

            var consumerConfiguration = new ConsumerConfiguration(connectionConfiguration.PrefetchCount);
            configure(consumerConfiguration);
            var consumer = consumerFactory.CreateConsumer(queue, (body, properties, receivedInfo) =>
            {
                var rawMessage = produceConsumeInterceptor.OnConsume(new RawMessage(properties, body));
                return onMessage(rawMessage.Body, rawMessage.Properties, receivedInfo);
            }, connection, consumerConfiguration);
            return consumer.StartConsuming();
        }

        // -------------------------------- publish ---------------------------------------------
        public virtual void Publish(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            MessageProperties messageProperties,
            byte[] body)
        {
            // Fix me: It's very hard now to move publish logic to separate abstraction, just leave it here. 
            var rawMessage = produceConsumeInterceptor.OnProduce(new RawMessage(messageProperties, body));
            if (connectionConfiguration.PublisherConfirms)
            {
                var timeBudget = new TimeBudget(TimeSpan.FromSeconds(connectionConfiguration.Timeout)).Start();
                while (!timeBudget.IsExpired())
                {
                    var confirmsWaiter = clientCommandDispatcher.Invoke(model =>
                    {
                        var properties = model.CreateBasicProperties();
                        rawMessage.Properties.CopyTo(properties);

                        var waiter = confirmationListener.GetWaiter(model);

                        try
                        {
                            model.BasicPublish(exchange.Name, routingKey, mandatory, properties, rawMessage.Body);
                        }
                        catch (Exception)
                        {
                            waiter.Cancel();
                            throw;
                        }

                        return waiter;
                    });

                    try
                    {
                        confirmsWaiter.Wait(timeBudget.GetRemainingTime());
                        break;
                    }
                    catch (PublishInterruptedException)
                    {
                    }
                }
            }
            else
            {
                clientCommandDispatcher.Invoke(model =>
                {
                    var properties = model.CreateBasicProperties();
                    rawMessage.Properties.CopyTo(properties);
                    model.BasicPublish(exchange.Name, routingKey, mandatory, properties, rawMessage.Body);
                });
            }
            eventBus.Publish(new PublishedMessageEvent(exchange.Name, routingKey, rawMessage.Properties, rawMessage.Body));
            logger.DebugWrite("Published to exchange: '{0}', routing key: '{1}', correlationId: '{2}'", exchange.Name, routingKey, messageProperties.CorrelationId);
        }

        public virtual void Publish<T>(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage<T> message) where T : class
        {

            var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
            Publish(exchange, routingKey, mandatory, serializedMessage.Properties, serializedMessage.Body);
        }

        public virtual Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage message)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(message, "message");

            var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
            return PublishAsync(exchange, routingKey, mandatory, serializedMessage.Properties, serializedMessage.Body);
        }

        public virtual Task PublishAsync<T>(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage<T> message) where T : class
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(message, "message");

            var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
            return PublishAsync(exchange, routingKey, mandatory, serializedMessage.Properties, serializedMessage.Body);
        }

        public virtual async Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            MessageProperties messageProperties,
            byte[] body)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(messageProperties, "messageProperties");
            Preconditions.CheckNotNull(body, "body");

            // Fix me: It's very hard now to move publish logic to separate abstraction, just leave it here. 
            var rawMessage = produceConsumeInterceptor.OnProduce(new RawMessage(messageProperties, body));
            if (connectionConfiguration.PublisherConfirms)
            {
                var timeBudget = new TimeBudget(TimeSpan.FromSeconds(connectionConfiguration.Timeout)).Start();
                while (!timeBudget.IsExpired())
                {
                    var confirmsWaiter = await clientCommandDispatcher.InvokeAsync(model =>
                    {
                        var properties = model.CreateBasicProperties();
                        rawMessage.Properties.CopyTo(properties);
                        var waiter = confirmationListener.GetWaiter(model);

                        try
                        {
                            model.BasicPublish(exchange.Name, routingKey, mandatory, properties, rawMessage.Body);
                        }
                        catch (Exception)
                        {
                            waiter.Cancel();
                            throw;
                        }

                        return waiter;
                    }).ConfigureAwait(false);

                    try
                    {
                        await confirmsWaiter.WaitAsync(timeBudget.GetRemainingTime()).ConfigureAwait(false);
                        break;
                    }
                    catch (PublishInterruptedException)
                    {
                    }
                }
            }
            else
            {
                await clientCommandDispatcher.InvokeAsync(model =>
                {
                    var properties = model.CreateBasicProperties();
                    rawMessage.Properties.CopyTo(properties);
                    model.BasicPublish(exchange.Name, routingKey, mandatory, properties, rawMessage.Body);
                }).ConfigureAwait(false);
            }
            eventBus.Publish(new PublishedMessageEvent(exchange.Name, routingKey, rawMessage.Properties, rawMessage.Body));
            logger.DebugWrite("Published to exchange: '{0}', routing key: '{1}', correlationId: '{2}'", exchange.Name, routingKey, messageProperties.CorrelationId);
        }

        // ---------------------------------- Exchange / Queue / Binding -----------------------------------
        public virtual IQueue QueueDeclare(
            string name,
            bool passive = false,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            int? perQueueMessageTtl = null,
            int? expires = null,
            int? maxPriority = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null,
            int? maxLength = null,
            int? maxLengthBytes = null)
        {
            Preconditions.CheckNotNull(name, "name");

            if (passive)
            {
                clientCommandDispatcher.Invoke(x => x.QueueDeclarePassive(name));
                return new Queue(name, exclusive);
            }

            var arguments = new Dictionary<string, object>();
            if (perQueueMessageTtl.HasValue)
            {
                arguments.Add("x-message-ttl", perQueueMessageTtl.Value);
            }
            if (expires.HasValue)
            {
                arguments.Add("x-expires", expires);
            }
            if (maxPriority.HasValue)
            {
                arguments.Add("x-max-priority", maxPriority.Value);
            }
            // Allow empty dead-letter-exchange as it represents the default rabbitmq exchange
            // and thus is a valid value. To dead-letter a message directly to a queue, you
            // would set dead-letter-exchange to empty and dead-letter-routing-key to name of the
            // queue since every queue has a direct binding with default exchange.
            if (deadLetterExchange != null)
            {
                arguments.Add("x-dead-letter-exchange", deadLetterExchange);
            }
            if (!string.IsNullOrEmpty(deadLetterRoutingKey))
            {
                arguments.Add("x-dead-letter-routing-key", deadLetterRoutingKey);
            }
            if (maxLength.HasValue)
            {
                arguments.Add("x-max-length", maxLength.Value);
            }
            if (maxLengthBytes.HasValue)
            {
                arguments.Add("x-max-length-bytes", maxLengthBytes.Value);
            }

            clientCommandDispatcher.Invoke(x => x.QueueDeclare(name, durable, exclusive, autoDelete, arguments));
            logger.DebugWrite("Declared Queue: '{0}', durable:{1}, exclusive:{2}, autoDelete:{3}, args:{4}"
                , name, durable, exclusive, autoDelete, string.Join(", ", arguments.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            return new Queue(name, exclusive);
        }

        public async Task<IQueue> QueueDeclareAsync(
            string name,
            bool passive = false,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            int? perQueueMessageTtl = null,
            int? expires = null,
            int? maxPriority = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null,
            int? maxLength = null,
            int? maxLengthBytes = null)
        {
            Preconditions.CheckNotNull(name, "name");

            if (passive)
            {
                await clientCommandDispatcher.InvokeAsync(x => x.QueueDeclarePassive(name)).ConfigureAwait(false);
                return new Queue(name, exclusive);
            }

            var arguments = new Dictionary<string, object>();
            if (perQueueMessageTtl.HasValue)
            {
                arguments.Add("x-message-ttl", perQueueMessageTtl.Value);
            }
            if (expires.HasValue)
            {
                arguments.Add("x-expires", expires);
            }
            if (maxPriority.HasValue)
            {
                arguments.Add("x-max-priority", maxPriority.Value);
            }
            // Allow empty dead-letter-exchange as it represents the default rabbitmq exchange
            // and thus is a valid value. To dead-letter a message directly to a queue, you
            // would set dead-letter-exchange to empty and dead-letter-routing-key to name of the
            // queue since every queue has a direct binding with default exchange.
            if (deadLetterExchange != null)
            {
                arguments.Add("x-dead-letter-exchange", deadLetterExchange);
            }
            if (!string.IsNullOrEmpty(deadLetterRoutingKey))
            {
                arguments.Add("x-dead-letter-routing-key", deadLetterRoutingKey);
            }
            if (maxLength.HasValue)
            {
                arguments.Add("x-max-length", maxLength.Value);
            }
            if (maxLengthBytes.HasValue)
            {
                arguments.Add("x-max-length-bytes", maxLengthBytes.Value);
            }

            await clientCommandDispatcher.InvokeAsync(x => x.QueueDeclare(name, durable, exclusive, autoDelete, arguments)).ConfigureAwait(false);
            logger.DebugWrite("Declared Queue: '{0}', durable:{1}, exclusive:{2}, autoDelete:{3}, args:{4}", name, durable, exclusive, autoDelete, string.Join(", ", arguments.Select(kvp =>
                $"{kvp.Key}={kvp.Value}")));
            return new Queue(name, exclusive);
        }

        public virtual void QueueDelete(IQueue queue, bool ifUnused = false, bool ifEmpty = false)
        {
            Preconditions.CheckNotNull(queue, "queue");

            clientCommandDispatcher.Invoke(x => x.QueueDelete(queue.Name, ifUnused, ifEmpty));
            logger.DebugWrite("Deleted Queue: {0}", queue.Name);
        }

        public virtual void QueuePurge(IQueue queue)
        {
            Preconditions.CheckNotNull(queue, "queue");

            clientCommandDispatcher.Invoke(x => x.QueuePurge(queue.Name));
            logger.DebugWrite("Purged Queue: {0}", queue.Name);
        }

        public virtual IExchange ExchangeDeclare(
            string name,
            string type,
            bool passive = false,
            bool durable = true,
            bool autoDelete = false,
            bool @internal = false,
            string alternateExchange = null,
            bool delayed = false)
        {

            Preconditions.CheckShortString(name, "name");
            Preconditions.CheckShortString(type, "type");

            if (passive)
            {
                clientCommandDispatcher.Invoke(x => x.ExchangeDeclarePassive(name));
                return new Exchange(name);
            }

            IDictionary<string, object> arguments = new Dictionary<string, object>();
            if (alternateExchange != null)
            {
                arguments.Add("alternate-exchange", alternateExchange);
            }
            if (delayed)
            {
                arguments.Add("x-delayed-type", type);
                type = "x-delayed-message";
            }
            clientCommandDispatcher.Invoke(x =>
            {
                x.ExchangeDeclare(name, type, durable, autoDelete, arguments);
            });
            logger.DebugWrite("Declared Exchange: {0} type:{1}, durable:{2}, autoDelete:{3}, delayed:{4}", name, type, durable, autoDelete, delayed);
            return new Exchange(name);
        }

        public async Task<IExchange> ExchangeDeclareAsync(
            string name,
            string type,
            bool passive = false,
            bool durable = true,
            bool autoDelete = false,
            bool @internal = false,
            string alternateExchange = null,
            bool delayed = false)
        {
            Preconditions.CheckShortString(name, "name");
            Preconditions.CheckShortString(type, "type");

            if (passive)
            {
                await clientCommandDispatcher.InvokeAsync(x => x.ExchangeDeclarePassive(name)).ConfigureAwait(false);
                return new Exchange(name);
            }
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            if (alternateExchange != null)
            {
                arguments.Add("alternate-exchange", alternateExchange);
            }
            if (delayed)
            {
                arguments.Add("x-delayed-type", type);
                type = "x-delayed-message";
            }
            await clientCommandDispatcher.InvokeAsync(x => x.ExchangeDeclare(name, type, durable, autoDelete, arguments)).ConfigureAwait(false);
            logger.DebugWrite("Declared Exchange: {0} type:{1}, durable:{2}, autoDelete:{3}, delayed:{4}", name, type, durable, autoDelete, delayed);
            return new Exchange(name);
        }

        public virtual void ExchangeDelete(IExchange exchange, bool ifUnused = false)
        {
            Preconditions.CheckNotNull(exchange, "exchange");

            clientCommandDispatcher.Invoke(x => x.ExchangeDelete(exchange.Name, ifUnused));
            logger.DebugWrite("Deleted Exchange: {0}", exchange.Name);
        }

        public virtual IBinding Bind(IExchange exchange, IQueue queue, string routingKey)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckShortString(routingKey, "routingKey");

            clientCommandDispatcher.Invoke(x => x.QueueBind(queue.Name, exchange.Name, routingKey, new Dictionary<string, object>()));
            logger.DebugWrite("Bound queue {0} to exchange {1} with routing key {2}", queue.Name, exchange.Name, routingKey);
            return new Binding(queue, exchange, routingKey);
        }

        public async Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckShortString(routingKey, "routingKey");

            await clientCommandDispatcher.InvokeAsync(x => x.QueueBind(queue.Name, exchange.Name, routingKey, new Dictionary<string, object>())).ConfigureAwait(false);
            logger.DebugWrite("Bound queue {0} to exchange {1} with routing key {2}", queue.Name, exchange.Name, routingKey);
            return new Binding(queue, exchange, routingKey);
        }

        public virtual IBinding Bind(IExchange source, IExchange destination, string routingKey)
        {
            Preconditions.CheckNotNull(source, "source");
            Preconditions.CheckNotNull(destination, "destination");
            Preconditions.CheckShortString(routingKey, "routingKey");

            clientCommandDispatcher.Invoke(x => x.ExchangeBind(destination.Name, source.Name, routingKey, new Dictionary<string, object>()));
            logger.DebugWrite("Bound destination exchange {0} to source exchange {1} with routing key {2}", destination.Name, source.Name, routingKey);
            return new Binding(destination, source, routingKey);
        }

        public async Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey)
        {
            Preconditions.CheckNotNull(source, "source");
            Preconditions.CheckNotNull(destination, "destination");
            Preconditions.CheckShortString(routingKey, "routingKey");

            await clientCommandDispatcher.InvokeAsync(x => x.ExchangeBind(destination.Name, source.Name, routingKey, new Dictionary<string, object>())).ConfigureAwait(false);
            logger.DebugWrite("Bound destination exchange {0} to source exchange {1} with routing key {2}", destination.Name, source.Name, routingKey);
            return new Binding(destination, source, routingKey);
        }

        public virtual void BindingDelete(IBinding binding)
        {
            Preconditions.CheckNotNull(binding, "binding");

            var queue = binding.Bindable as IQueue;
            if (queue != null)
            {
                clientCommandDispatcher.Invoke(x => x.QueueUnbind(queue.Name, binding.Exchange.Name, binding.RoutingKey, null));
                logger.DebugWrite("Unbound queue {0} from exchange {1} with routing key {2}", queue.Name, binding.Exchange.Name, binding.RoutingKey);
            }
            else
            {
                var destination = binding.Bindable as IExchange;
                if (destination == null)
                    return;
                clientCommandDispatcher.InvokeAsync(x => x.ExchangeUnbind(destination.Name, binding.Exchange.Name, binding.RoutingKey, new Dictionary<string, object>()));
                logger.DebugWrite("Unbound destination exchange {0} from source exchange {1} with routing key {2}", destination.Name, binding.Exchange.Name, binding.RoutingKey);
            }
        }

        public IBasicGetResult<T> Get<T>(IQueue queue) where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            var result = Get(queue);
            if (result?.Body == null)
            {
                logger.DebugWrite("... but no message was available on queue '{0}'", queue.Name);
                return new BasicGetResult<T>();
            }
            var message = messageSerializationStrategy.DeserializeMessage<T>(result.Properties, result.Body);
            if (message.MessageType == typeof(T))
            {
                return new BasicGetResult<T>(message);
            }
            logger.ErrorWrite("Incorrect message type returned from Get. Expected {0}, but was {1}", typeof(T).Name, message.MessageType.Name);
            throw new MessageBusException("Incorrect message type returned from Get. Expected {0}, but was {1}", typeof(T).Name, message.MessageType.Name);
        }

        public IBasicGetResult Get(IQueue queue)
        {
            Preconditions.CheckNotNull(queue, "queue");

            //var result = clientCommandDispatcher.Invoke(x => x.BasicGet(queue.Name, false));
            RabbitMQ.Client.IModel model = connection.CreateModel();
            model.ContinuationTimeout = connectionConfiguration.ContinuationTimeout;
            var result = model.BasicGet(queue.Name, false);
        
            if (result == null)
                return null;
            else 
                model.BasicAck(result.DeliveryTag, false);

            var getResult = new BasicGetResult(
                result.Body,
                new MessageProperties(result.BasicProperties),
                new MessageReceivedInfo(
                    "",
                    result.DeliveryTag,
                    result.Redelivered,
                    result.Exchange,
                    result.RoutingKey,
                    queue.Name
                    )
                );

            logger.DebugWrite("Message Get from queue '{0}'", queue.Name);

            return getResult;
        }

        public uint MessageCount(IQueue queue)
        {
            Preconditions.CheckNotNull(queue, "queue");
            var messageCount = clientCommandDispatcher.Invoke(x => x.QueueDeclarePassive(queue.Name)).MessageCount;

            logger.DebugWrite("{0} messages in queue '{1}'", messageCount, queue.Name);
            return messageCount;
        }

        //------------------------------------------------------------------------------------------
        public virtual event EventHandler Connected;

        protected void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);
        
        public virtual event EventHandler Disconnected;

        protected void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);

        public virtual event EventHandler<ConnectionBlockedEventArgs> Blocked;

        protected void OnBlocked(ConnectionBlockedEvent args) => Blocked?.Invoke(this, new ConnectionBlockedEventArgs(args.Reason));

        public virtual event EventHandler Unblocked;

        protected void OnUnblocked() => Unblocked?.Invoke(this, EventArgs.Empty);

        public virtual event EventHandler<MessageReturnedEventArgs> MessageReturned;

        protected void OnMessageReturned(ReturnedMessageEvent args) => MessageReturned?.Invoke(this, new MessageReturnedEventArgs(args.Body, args.Properties, args.Info));

        public virtual bool IsConnected => connection.IsConnected;

        public IContainer Container { get; }

        public IConventions Conventions { get; }

        private bool disposed;

        public virtual void Dispose()
        {
            if (disposed) return;

            consumerFactory.Dispose();
            confirmationListener.Dispose();
            clientCommandDispatcher.Dispose();
            connection.Dispose();

            disposed = true;

            logger.DebugWrite("Connection disposed");
        }
    }
}
