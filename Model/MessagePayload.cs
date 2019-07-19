using Newtonsoft.Json;
using System;

namespace StoryBot.Vk.Model
{
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
}
