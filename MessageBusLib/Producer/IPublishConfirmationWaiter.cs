using System;
using System.Threading.Tasks;

namespace MessageBus.Producer
{
    public interface IPublishConfirmationWaiter
    {
        void Wait(TimeSpan timeout);
        Task WaitAsync(TimeSpan timeout);
        void Cancel();
    }
}