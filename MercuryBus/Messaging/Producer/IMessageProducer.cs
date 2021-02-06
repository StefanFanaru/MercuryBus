using System.Threading.Tasks;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Messaging.Producer
{
	/// <summary>
	///     Supports sending basic messages
	/// </summary>
	public interface IMessageProducer
    {
	    /// <summary>
	    ///     Send a message
	    /// </summary>
	    /// <param name="destination">The destination channel</param>
	    /// <param name="message">The message to send</param>
	    void Send(string destination, IMessage message);

	    /// <summary>
	    ///     Send a message
	    /// </summary>
	    /// <param name="destination">The destination channel</param>
	    /// <param name="message">The message to send</param>
	    Task SendAsync(string destination, IMessage message);
    }
}