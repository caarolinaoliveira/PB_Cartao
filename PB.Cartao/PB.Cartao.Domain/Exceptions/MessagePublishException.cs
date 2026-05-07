namespace PB.Cartao.Domain.Exceptions
{
    public class MessagePublishException : Exception
    {
        public MessagePublishException(string message) : base(message) { }
        public MessagePublishException(string message, Exception inner) : base(message, inner) { }
    }
}