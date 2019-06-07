using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class StoryDocument
    {
        [BsonId]
        public ObjectId ObjectId { get; set; }

        [BsonElement("tag")]
        public string Tag { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("story")]
        public Storyline[] Story { get; set; }

        [BsonElement("beginning")]
        public string Beginning { get; set; }

        [BsonElement("endings")]
        public StoryEnding[] Endings { get; set; }
    }
}
