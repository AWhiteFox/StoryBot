using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Vk.Model;
using StoryBot.Vk.Vk.Logic;
using System;
using VkNet.Model;
using VkNet.Model.GroupUpdate;
using VkNet.Utils;

namespace StoryBot.Vk.Logic
{
    public class EventsHandler
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly VkReplyHandler reply;

        public EventsHandler(VkReplyHandler reply)
        {
            this.reply = reply;
        }

        public void MessageNewEvent(JObject jObject)
        {
            var message = Message.FromJson(new VkResponse(jObject));
            var peerId = message.PeerId.Value;

            if (reply.CheckThatMessageIsLast(message))
            {
                try
                {
                    if (!string.IsNullOrEmpty(message.Payload))
                    {
                        var payload = JsonConvert.DeserializeObject<MessagePayload>(message.Payload);
                        if (!string.IsNullOrEmpty(payload.Button))
                        {
                            reply.ReplyToNumber(peerId, int.Parse(payload.Button));
                        }
                        else if (payload.Command == "start")
                        {
                            reply.ReplyFirstMessage(peerId);
                        }
                    }
                    else if (!string.IsNullOrEmpty(message.Text))
                    {
                        if (message.Text[0] == reply.Prefix)
                        {
                            reply.ReplyToCommand(peerId, message.Text.Remove(0, 1).ToLower());
                        }
                        else if (int.TryParse(message.Text, out int number))
                        {
                            reply.ReplyToNumber(peerId, number);
                        }
                        else if (message.Text.ToLower() == "начать")
                        {
                            reply.ReplyFirstMessage(peerId);
                        }
                    }
                }
                catch (Exception exception)
                {
                    reply.ReplyWithError(peerId, exception);
                    throw;
                }
            }
            else
            {
                logger.Debug($"Ignoring old message ({message.Date.ToString()}) from {message.PeerId}");
            }
        }

        public void MessageAllowEvent(JObject jObject)
        {
            reply.ReplyFirstMessage(MessageAllow.FromJson(new VkResponse(jObject)).UserId.Value);
        }
    }
}
