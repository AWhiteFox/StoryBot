using Newtonsoft.Json;
using System;

namespace StoryBot.Model
{
    [Serializable]
    public class Payload
    {
        [JsonProperty("button")]
        public string Button { get; set; }
    }
}
