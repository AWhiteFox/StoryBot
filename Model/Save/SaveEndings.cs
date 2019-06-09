using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class SaveEndings
    {
        [BsonElement("id")]
        public int StoryId { get; set; }

        [BsonElement("obtained")]
        public int[] ObtainedEndings { get; set; }
    }
}
