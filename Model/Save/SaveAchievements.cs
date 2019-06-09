using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class SaveAchievements
    {
        [BsonElement("id")]
        public int StoryId { get; set; }

        [BsonElement("obtained")]
        public int[] ObtainedAchievements { get; set; }
    }
}
