using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class SaveAchievements
    {
        [BsonElement("tag")]
        public string QuestTag { get; set; }

        [BsonElement("obtained")]
        public int[] ObtainedAchievements { get; set; }
    }
}
