namespace MessageBus
{
    public interface ICorrelationIdGenerationStrategy
    {
        string GetCorrelationId();
    }
}