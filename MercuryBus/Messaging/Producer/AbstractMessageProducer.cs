using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MercuryBus.Messaging.Common;
using Microsoft.Extensions.Logging;

namespace MercuryBus.Messaging.Producer
{
    public abstract class AbstractMessageProducer
    {
        protected readonly ILogger<AbstractMessageProducer> Logger;
        protected readonly IMessageInterceptor[] MessageInterceptors;

        protected AbstractMessageProducer(ILogger<AbstractMessageProducer> logger,
            IEnumerable<IMessageInterceptor> messageInterceptors)
        {
            Logger = logger;
            MessageInterceptors = messageInterceptors.ToArray();
        }

        protected void PreSend(IMessage message)
        {
            var logContext = $"{nameof(PreSend)} message.Id={message.Id}";
            Logger.LogDebug($"+{logContext}");

            foreach (var messageInterceptor in MessageInterceptors)
            {
                messageInterceptor.PreSend(message);
            }

            Logger.LogDebug($"-{logContext}: sent to {MessageInterceptors.Length} message interceptors");
        }

        protected void PostSend(IMessage message, Exception e)
        {
            var logContext = $"{nameof(PostSend)} message.Id={message.Id}";
            Logger.LogDebug($"+{logContext}");
            foreach (var messageInterceptor in MessageInterceptors)
            {
                messageInterceptor.PostSend(message, e);
            }

            Logger.LogDebug($"-{logContext}: sent to {MessageInterceptors.Length} message interceptors");
        }

        protected void SendMessage(string id, string destination, IMessage message, IMessageSender messageSender)
        {
            var logContext = $"{nameof(SendMessage)} id='{id}', destination='{destination}";
            Logger.LogDebug($"+{logContext}");

            EnsureIdHeaderIsSet(id, message, logContext);

            message.SetHeader(MessageHeaders.Destination, destination);
            message.SetHeader(MessageHeaders.Date, HttpDateHeaderFormatUtil.NowAsHttpDateString());

            PreSend(message);
            try
            {
                messageSender.Send(message);
                PostSend(message, null);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"{logContext}: Exception while sending message");
                throw;
            }
        }

        protected async Task SendMessageAsync(string id, string destination, IMessage message, IMessageSender messageSender)
        {
            var logContext = $"{nameof(SendMessageAsync)} id='{id}', destination='{destination}'";
            Logger.LogDebug($"+{logContext}");

            EnsureIdHeaderIsSet(id, message, logContext);

            message.SetHeader(MessageHeaders.Destination, destination);
            message.SetHeader(MessageHeaders.Date, HttpDateHeaderFormatUtil.NowAsHttpDateString());

            PreSend(message);
            try
            {
                await messageSender.SendAsync(message);
                PostSend(message, null);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"{logContext}: Exception sending message");
                throw;
            }
        }

        /// <summary>
        ///     Will ensure that the message has an ID header
        /// </summary>
        private void EnsureIdHeaderIsSet(string id, IMessage message, string logContext)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                if (message.GetHeader(MessageHeaders.Id) == null)
                {
                    Logger.LogError($"{logContext}: Message is missing ID header");
                    throw new ArgumentNullException(nameof(id), "Message needs an ID header");
                }
            }
            else
            {
                message.SetHeader(MessageHeaders.Id, id);
            }
        }
    }
}