using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace StoryBot.Model
{
    [Serializable]
    public class CallbackUpdate
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("object")]
        public JObject Object { get; set; }

        [JsonProperty("group_id")]
        public long GroupId { get; set; }
    }
}
