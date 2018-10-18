using System;

namespace MessageBus
{
    public class MessageBusException : Exception
    {
        public MessageBusException() {}
        public MessageBusException(string message) : base(message) {}
        public MessageBusException(string format, params string[] args) : base(string.Format(format, args)) {}
        public MessageBusException(string message, Exception inner) : base(message, inner) {}
    }

    public class MessageBusExceptionInvalidMessageTypeException : MessageBusException
    {
        public MessageBusExceptionInvalidMessageTypeException() {}
        public MessageBusExceptionInvalidMessageTypeException(string message) : base(message) {}
        public MessageBusExceptionInvalidMessageTypeException(string format, params string[] args) : base(format, args) {}
        public MessageBusExceptionInvalidMessageTypeException(string message, Exception inner) : base(message, inner) {}
    }

    public class MessageBusResponderException : MessageBusException
    {
        public MessageBusResponderException() { }
        public MessageBusResponderException(string message) : base(message) { }
        public MessageBusResponderException(string format, params string[] args) : base(format, args) { }
        public MessageBusResponderException(string message, Exception inner) : base(message, inner) { }
    }
}