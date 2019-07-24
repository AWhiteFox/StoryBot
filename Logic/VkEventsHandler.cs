using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Core.Logic;
using System;
using System.Linq;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.RequestParams;
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
        /// VK API
        /// </summary>
        private readonly IVkApi vkApi;
        
        /// <summary>
        /// Reply Handler
        /// </summary>
        private readonly ReplyHandler<MessagesSendParams> replyHandler;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="reply"></param>
        public VkEventsHandler(IVkApi vkApi, 
                               ReplyHandler<MessagesSendParams> reply)
        {
            this.vkApi = vkApi;
            this.replyHandler = reply;
        }

        #region Callback Events

        /// <summary>
        /// New message event
        /// </summary>
        /// <param name="jObject"></param>
        public void MessageNewEvent(JObject jObject)
        {
            var message = Message.FromJson(new VkResponse(jObject));
            var peerId = message.PeerId.Value;

            if (CheckThatMessageIsLast(message)) // Check that message is last
            {
                try // Try-catch: If something goes wrong reply error and throw
                {
                    if (!string.IsNullOrEmpty(message.Payload)) // Check for payload
                    {
                        var payload = JsonConvert.DeserializeObject<MessagePayload>(message.Payload);
                        if (!string.IsNullOrEmpty(payload.Button)) // If payload contains info about pressed button...
                        {
                            replyHandler.ReplyToNumber(peerId, int.Parse(payload.Button));
                        }
                        else if (payload.Command == "start") // For VK "Begin" button
                        {
                            replyHandler.ReplyToTheFirstMessage(peerId);
                        }
                    }
                    else if (!string.IsNullOrEmpty(message.Text)) // If message text is not empty
                    {
                        if (message.Text[0] == replyHandler.Prefix) // Prefix check
                        {
                            HandleCommand(peerId, message.Text);
                        }
                        else if (int.TryParse(message.Text, out int number)) // Number check
                        {
                            replyHandler.ReplyToNumber(peerId, number);
                        }
                        else if (message.Text.ToLower() == "начать") // "Begin" message (not VK button) check
                        {
                            replyHandler.ReplyToTheFirstMessage(peerId);
                        }
                    }
                }
                catch (Exception exception) // If something went wrong...
                {
                    replyHandler.ReplyWithError(peerId, exception.Message);
                    throw;
                }
            }
            else
            {
                logger.Debug($"Ignoring old message ({message.Date.ToString()}) from {message.PeerId}");
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Checks if provided message is the last message in conversation
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool CheckThatMessageIsLast(Message message) =>
            vkApi.Messages.GetHistory(new MessagesGetHistoryParams { Count = 1, PeerId = message.PeerId }).Messages.Single().Date <= message.Date;

        /// <summary>
        /// Handles commands
        /// </summary>
        /// <returns></returns>
        private void HandleCommand(long peerId, string message)
        {
            // Try-catch: If something goes wrong send "Wrong command usage" message
            try
            {
                // Split command to alias and arguments
                string[] splittedCommand = message.Remove(0, 1).ToLower().Split(" ");
                string alias = splittedCommand[0];
                string[] args = splittedCommand.Skip(1).ToArray();

                switch (alias)
                {
                    case "list":
                        switch (args.Length)
                        {
                            case 0: replyHandler.Commands.List(peerId); return;
                            case 1:
                                {
                                    // Parse story ID from first argument
                                    var storyId = int.Parse(args[0]);
                                    replyHandler.Commands.List(peerId, storyId);
                                    return;
                                }
                            case 2:
                                {
                                    // Parse story ID from first argument
                                    var storyId = int.Parse(args[0]);
                                    // Parse episode ID from second argument
                                    var episodeId = int.Parse(args[1]);
                                    replyHandler.Commands.List(peerId, storyId, episodeId);
                                    return;
                                }
                        }
                        goto WrongCommandUsage;
                    case "repeat": replyHandler.Commands.Repeat(peerId); return;
                    case "select": replyHandler.Commands.Select(peerId); return;
                    default: replyHandler.ReplyWithCommandList(peerId); return;
                }
            }
            catch (Exception) { goto WrongCommandUsage; }

        WrongCommandUsage:
            replyHandler.ReplyWithError(peerId, "Неправильное использование команды.");
        }

        #endregion

        #region Private Nested Classes

        /// <summary>
        /// Payload of VK Message
        /// </summary>
        [Serializable]
        public class MessagePayload
        {
            [JsonProperty("button")]
            public string Button { get; set; }

            [JsonProperty("command")]
            public string Command { get; set; }
        }

        #endregion
    }
}
