using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class StoryOption
    {
        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("storyline")]
        public string Storyline { get; set; }

        [BsonElement("position")]
        public int Position { get; set; }

        [BsonElement("achievement")]
        public int? Achievement { get; set; }
    }
}
