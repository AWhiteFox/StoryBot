using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class StorylineElement
    {
        [BsonElement("content")]
        public string[] Content { get; set; }

        [BsonElement("options")]
        public StoryOption[] Options { get; set; }
    }
}
