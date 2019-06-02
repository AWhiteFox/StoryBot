using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class StoryProgress
    {
        [BsonElement("storyl")]
        public string Story { get; set; }

        [BsonElement("storyline")]
        public string Storyline { get; set; }

        [BsonElement("position")]
        public int Position { get; set; }
    }
}
