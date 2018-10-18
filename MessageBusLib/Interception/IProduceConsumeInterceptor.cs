namespace MessageBus.Interception
{
    public interface IProduceConsumeInterceptor
    {
        RawMessage OnProduce(RawMessage rawMessage);
        RawMessage OnConsume(RawMessage rawMessage);
    }

}