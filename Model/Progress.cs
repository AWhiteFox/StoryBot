using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;

namespace StoryBot.Model
{
    [Serializable]
    public class Progress
    {
        [JsonProperty("story")]
        [BsonElement("story")]
        public string Story { get; set; }

        [JsonProperty("storyline")]
        [BsonElement("storyline")]
        public string Storyline { get; set; }

        [JsonProperty("position")]
        [BsonElement("position")]
        public int Position { get; set; }
    }
}
