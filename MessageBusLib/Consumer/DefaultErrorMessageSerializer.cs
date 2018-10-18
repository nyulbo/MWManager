using System.Text;

namespace MessageBus.Consumer
{
    public class DefaultErrorMessageSerializer : IErrorMessageSerializer
    {
        public string Serialize(byte[] messageBody)
        {
            return Encoding.UTF8.GetString(messageBody);
        }

        public byte[] Deserialize(string messageBody)
        {
            return Encoding.UTF8.GetBytes(messageBody);
        }
    }
}
