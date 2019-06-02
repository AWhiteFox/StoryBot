using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class Storyline
    {
        [BsonElement("tag")]
        public string Tag { get; set; }

        [BsonElement("elements")]
        public StorylineElement[] Elements { get; set; }
    }
}
