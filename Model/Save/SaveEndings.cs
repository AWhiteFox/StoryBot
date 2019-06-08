using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class SaveEndings
    {
        [BsonElement("tag")]
        public string QuestTag { get; set; }

        [BsonElement("obtained")]
        public int[] ObtainedEndings { get; set; }
    }
}
