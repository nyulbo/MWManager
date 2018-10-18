using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Events;
using MessageBus.Internals;
using RabbitMQ.Client;

namespace MessageBus.Producer
{
    public class PublishConfirmationListener : IPublishConfirmationListener
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly ConcurrentDictionary<IModel, ConcurrentDictionary<ulong, TaskCompletionSource<object>>> unconfirmedChannelRequests = new ConcurrentDictionary<IModel, ConcurrentDictionary<ulong, TaskCompletionSource<object>>>();

        public PublishConfirmationListener(IEventBus eventBus)
        {
            eventBus.Subscribe<MessageConfirmationEvent>(OnMessageConfirmation);
            eventBus.Subscribe<PublishChannelCreatedEvent>(OnPublishChannelCreated);
        }

        public IPublishConfirmationWaiter GetWaiter(IModel model)
        {
            var deliveryTag = model.NextPublishSeqNo;
            var requests = unconfirmedChannelRequests.GetOrAdd(model, new ConcurrentDictionary<ulong, TaskCompletionSource<object>>());
            var confirmation = new TaskCompletionSource<object>();
            requests.Add(deliveryTag, confirmation);
            return new PublishConfirmationWaiter(deliveryTag, confirmation.Task, cancellation.Token, () => requests.Remove(deliveryTag));
        }

        public void Dispose()
        {
            cancellation.Cancel();
        }

        private void OnMessageConfirmation(MessageConfirmationEvent @event)
        {
            ConcurrentDictionary<ulong, TaskCompletionSource<object>> requests;
            if (!unconfirmedChannelRequests.TryGetValue(@event.Channel, out requests))
            {
                return;
            }

            var deliveryTag = @event.DeliveryTag;
            var multiple = @event.Multiple;
            var isNack = @event.IsNack;
            if (multiple)
            {
                // Fix me: ConcurrentDictionary.Keys acquires all locks, it is very expensive operation and could perform slowly.
                foreach (var sequenceNumber in requests.Keys.Where(x => x <= deliveryTag))
                {
                    TaskCompletionSource<object> confirmation;
                    if (requests.TryGetValue(sequenceNumber, out confirmation))
                    {
                        Confirm(confirmation, sequenceNumber, isNack);
                    }
                }
            }
            else
            {
                TaskCompletionSource<object> confirmation;
                if (requests.TryGetValue(deliveryTag, out confirmation))
                {
                    Confirm(confirmation, deliveryTag, isNack);
                }
            }
        }

        private void OnPublishChannelCreated(PublishChannelCreatedEvent @event)
        {
            foreach (var channel in unconfirmedChannelRequests.Keys)
            {
                ConcurrentDictionary<ulong, TaskCompletionSource<object>> confirmations;
                if (!unconfirmedChannelRequests.TryRemove(channel, out confirmations))
                {
                    continue;
                }
                foreach (var deliveryTag in confirmations.Keys)
                {
                    TaskCompletionSource<object> confirmation;
                    if (!confirmations.TryRemove(deliveryTag, out confirmation))
                    {
                        continue;
                    }
                    confirmation.TrySetExceptionSafe(new PublishInterruptedException());
                }
            }
            unconfirmedChannelRequests.Add(@event.Channel, new ConcurrentDictionary<ulong, TaskCompletionSource<object>>());
        }

        private static void Confirm(TaskCompletionSource<object> tcs, ulong deliveryTag, bool isNack)
        {
            if (isNack)
            {
                tcs.TrySetExceptionSafe(new PublishNackedException(string.Format("Broker has signalled that publish {0} was unsuccessful", deliveryTag)));
            }
            else
            {
                tcs.TrySetResultSafe(null);
            }
        }
    }
}