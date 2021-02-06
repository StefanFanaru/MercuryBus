using System;
using MercuryBus.Messaging.Common;

namespace MercuryBus.Messaging.Consumer
{
    public delegate void MessageHandler(IMessage message, IServiceProvider serviceProvider);
}