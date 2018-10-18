using System;
using System.Threading.Tasks;
using MessageBus.Topology;

namespace MessageBus.Producer
{
    /// <summary>
    /// An RPC style request-response pattern
    /// </summary>
    public interface IRpc
    {   
        /// <summary>
        /// Make a request to an RPC service
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="request">The request message</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class;

        Task<byte[]> Request(byte[] request, MessageConfiguration messageConfig);      

        /// <summary>
        /// Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">A function that performs the response</param>
        IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class;


        /// <summary>
        /// Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">A function that performs the response</param>
        /// <param name="configure">A function that performs the configuration</param>
        IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure)
            where TRequest : class
            where TResponse : class;

        IDisposable Respond(Func<byte[], Task<byte[]>> responder, Action<IResponderConfiguration> configure, string queueName);
    }
}