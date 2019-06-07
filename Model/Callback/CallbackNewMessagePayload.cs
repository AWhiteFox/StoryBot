using Newtonsoft.Json;
using System;

namespace StoryBot.Model
{
    [Serializable]
    public class CallbackNewMessagePayload
    {
        [JsonProperty("button")]
        public string Button { get; set; }
    }
}
