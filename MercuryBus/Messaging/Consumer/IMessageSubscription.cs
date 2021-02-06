namespace MercuryBus.Messaging.Consumer
{
	/// <summary>
	///     Message subscription handle to allow subscriber to unsubscribe
	/// </summary>
	public interface IMessageSubscription
    {
	    /// <summary>
	    ///     Unsubscribe from the subscription
	    /// </summary>
	    void Unsubscribe();
    }
}