using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Vk.Model;
using StoryBot.Vk.Vk.Logic;
using System;
using VkNet.Model;
using VkNet.Utils;

namespace StoryBot.Vk.Logic
{
    public class VkEventsHandler
    {
        /// <summary>
        /// Logger
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Reply Handler
        /// </summary>
        private readonly VkReplyHandler reply;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="reply"></param>
        public VkEventsHandler(VkReplyHandler reply)
        {
            this.reply = reply;
        }

        /// <summary>
        /// New message event
        /// </summary>
        /// <param name="jObject"></param>
        public void MessageNewEvent(JObject jObject)
        {
            var message = Message.FromJson(new VkResponse(jObject));
            var peerId = message.PeerId.Value;

            if (reply.CheckThatMessageIsLast(message)) // Check that message is last
            {
                try // Try-catch: If something goes wrong reply error and throw
                {
                    if (!string.IsNullOrEmpty(message.Payload)) // Check for payload
                    {
                        var payload = JsonConvert.DeserializeObject<MessagePayload>(message.Payload);
                        if (!string.IsNullOrEmpty(payload.Button)) // If payload contains info about pressed button...
                        {
                            reply.ReplyToNumber(peerId, int.Parse(payload.Button));
                        }
                        else if (payload.Command == "start") // For VK "Begin" button
                        {
                            reply.ReplyFirstMessage(peerId);
                        }
                    }
                    else if (!string.IsNullOrEmpty(message.Text)) // If message text is not empty
                    {
                        if (message.Text[0] == reply.Prefix) // Prefix check
                        {
                            reply.ReplyToCommand(peerId, message.Text.Remove(0, 1).ToLower());
                        }
                        else if (int.TryParse(message.Text, out int number)) // Number check
                        {
                            reply.ReplyToNumber(peerId, number);
                        }
                        else if (message.Text.ToLower() == "начать") // "Begin" message (not VK button) check
                        {
                            reply.ReplyFirstMessage(peerId);
                        }
                    }
                }
                catch (Exception exception) // If something went wrong...
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
    }
}
