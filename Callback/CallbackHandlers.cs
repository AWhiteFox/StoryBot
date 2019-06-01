using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Messaging;
using System;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Utils;

namespace StoryBot.Callback
{
    public class CallbackHandler
    {
        private readonly MessageHandler messageHandler;

        public CallbackHandler(IVkApi api, IMongoDatabase database)
        {
            messageHandler = new MessageHandler(api, database);
        }

        public void NewMessage(JObject obj)
        {
            var content = Message.FromJson(new VkResponse(obj));
            long peerId = content.PeerId.Value;

            if (content.Text[0] == '!')
            {
                switch (content.Text.ToLower())
                {
                    case "!helloworld":
                        messageHandler.SendHelloWorld(peerId);
                        return;
                    case "!reset":
                        messageHandler.SendMenu(peerId);
                        return;
                    default:
                        return;
                }
            }
            else if (content.Payload != null)
            {
                messageHandler.HandleKeyboard(peerId, JsonConvert.DeserializeObject<Payload>(content.Payload).Button);
            }
            else
            {
                return;
            }
        }

        [Serializable]
        private class Payload
        {
            [JsonProperty("button")]
            public string Button { get; set; }
        }
    }
}
