using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class StoryDocument
    {
        [BsonId]
        public ObjectId ObjectId { get; set; }

        [BsonElement("id")]
        public int Id { get; set; }

        [BsonElement("chapter")]
        public int Chapter { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("storylines")]
        public Storyline[] Storylines { get; set; }

        [BsonElement("beginning")]
        public string Beginning { get; set; }

        [BsonElement("endings")]
        public StoryEnding[] Endings { get; set; }

        [BsonElement("achievements")]
        public StoryAchievement[] Achievements { get; set; }
    }
}
