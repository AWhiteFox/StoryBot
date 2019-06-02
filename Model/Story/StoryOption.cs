using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class StoryOption
    {
        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("next")]
        public string Next { get; set; }

        [BsonElement("next_pos")]
        public int NextPosition { get; set; }
    }
}
