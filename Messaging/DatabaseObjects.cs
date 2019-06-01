using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Messaging
{
    public static class DatabaseObjects
    {
        public class StoryDocument
        {
            [BsonElement("_id")]
            public ObjectId MongoId { get; set; }
            [BsonElement("tag")]
            public string Tag { get; set; }
            [BsonElement("name")]
            public string Name { get; set; }
            [BsonElement("story")]
            public object Story { get; set; }
            [BsonElement("beginning")]
            public string Beginning { get; set; }
            [BsonElement("endings")]
            public object[] Endings { get; set; }
        }

        public class StorylineElement
        {
            [BsonElement("content")]
            public string[] Content { get; set; }
            [BsonElement("options")]
            public StoryOption[] Options { get; set; }
        }
        
        public class StoryOption
        {
            [BsonElement("content")]
            public string Content { get; set; }
            [BsonElement("next")]
            public string Next { get; set; }
            [BsonElement("next_pos")]
            public int NextPosition { get; set; }
        }

        public class SaveDocument
        {
            [BsonElement("_id")]
            public ObjectId MongoId { get; set; }
            [BsonElement("id")]
            public long? Id { get; set; }
            [BsonElement("current")]
            public StoryProgress Current { get; set; }
            [BsonElement("endings")]
            public object Endings { get; set; }
        }

        public class StoryProgress
        {
            [BsonElement("story")]
            public string Story { get; set; }
            [BsonElement("storyline")]
            public string Storyline { get; set; }
            [BsonElement("position")]
            public int Position { get; set; }
        }

        public class Ending
        {
            [BsonElement("type")]
            public int Type { get; set; }
            [BsonElement("Name")]
            public string Name { get; set; }
            [BsonElement("content")]
            public string[] Content { get; set; }
        }
    }
}
