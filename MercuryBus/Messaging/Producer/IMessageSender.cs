using System.Threading.Tasks;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Messaging.Producer
{
    public interface IMessageSender
    {
        void Send(IMessage message);

        Task SendAsync(IMessage message);
    }
}