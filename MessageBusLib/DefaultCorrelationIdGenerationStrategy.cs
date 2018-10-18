using System;

namespace MessageBus
{
    public class DefaultCorrelationIdGenerationStrategy : ICorrelationIdGenerationStrategy
    {
        public string GetCorrelationId()
        {
            return Guid.NewGuid().ToString();
        } 
    }
}