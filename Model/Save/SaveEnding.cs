using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class SaveEnding
    {
        [BsonElement("tag")]
        public string QuestTag { get; set; }

        [BsonElement("obtained")]
        public int[] ObtainedEndings { get; set; }
    }
}
