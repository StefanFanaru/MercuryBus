using System.Collections.Generic;

namespace MercuryBus.Messaging.Common
{
    public interface IMessage
    {
        string Id { get; }
        IDictionary<string, string> Headers { get; }
        string Payload { get; set; }
        string GetHeader(string name);
        string GetRequiredHeader(string name);
        bool HasHeader(string name);
        void SetHeader(string name, string value);
        void RemoveHeader(string name);
    }
}