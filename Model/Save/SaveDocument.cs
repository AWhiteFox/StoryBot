using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class SaveDocument
    {
        [BsonId]
        public ObjectId ObjectId { get; set; }
        
        [BsonElement("id")]
        public long Id { get; set; }

        [BsonElement("current")]
        public SaveProgress Current { get; set; }

        [BsonElement("endings")]
        public SaveEndings[] Endings { get; set; }

        [BsonElement("achievements")]
        public SaveAchievements[] Achievements { get; set; }
    }
}
