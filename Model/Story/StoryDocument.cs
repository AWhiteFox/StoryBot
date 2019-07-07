using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace StoryBot.Model
{
    public class StoryDocument
    {
        [BsonId]
        public ObjectId ObjectId { get; set; }

        [BsonElement("id")]
        public int StoryId { get; set; }

        [BsonElement("episode")]
        public int Episode { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("storylines")]
        public Storyline[] Storylines { get; set; }

        [BsonElement("endings")]
        public StoryEnding[] Endings { get; set; } = Array.Empty<StoryEnding>();

        [BsonElement("achievements")]
        public StoryAchievement[] Achievements { get; set; } = Array.Empty<StoryAchievement>();
    }
}
