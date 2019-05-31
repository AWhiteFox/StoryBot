using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Messaging;
using System;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace StoryBot.Callback
{
    public class CallbackHandler
    {
        public IVkApi vkApi;
        public MessageHandler messageHandler;

        public CallbackHandler(IVkApi api, IMongoDatabase database)
        {
            vkApi = api;
            messageHandler = new MessageHandler(database);
        }

        public void NewMessage(JObject obj)
        {
            var content = Message.FromJson(new VkResponse(obj));
            MessagesSendParams response = new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = content.PeerId.Value
            };

            if (content.Text[0] == '!')
            {
                switch (content.Text.ToLower())
                {
                    case "!helloworld":
                        response.Message = "Hello, World!";
                        break;
                    case "!menu":
                        messageHandler.SendMenu(ref response);
                        break;
                    default:
                        return;
                }
            }
            else if (content.Payload != null)
            {
                messageHandler.HandleKeyboard(ref response, JsonConvert.DeserializeObject<Payload>(content.Payload).Button, content.UserId);
            }
            else
            {
                return;
            }

            vkApi.Messages.Send(response);
        }

        [Serializable]
        private class Payload
        {
            [JsonProperty("button")]
            public string Button { get; set; }
        }
    }
}
