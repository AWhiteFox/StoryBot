using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace StoryBot.Model
{
    public class SaveProgress
    {
        [BsonConstructor]
        public SaveProgress()
        {
            Position = 0;
            Unlockables = new List<string>();
        }

        [BsonElement("story")]
        public int? Story { get; set; }

        [BsonElement("episode")]
        public int? Episode { get; set; }

        [BsonElement("storyline")]
        public string Storyline { get; set; }

        [BsonElement("position")]
        public int Position { get; set; }

        [BsonElement("unlockables")]
        public List<string> Unlockables { get; set; }
    }
}
