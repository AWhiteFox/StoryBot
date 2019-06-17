using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class SaveEndings
    {
        [BsonElement("story_id")]
        public int StoryId { get; set; }

        [BsonElement("chapter_id")]
        public int ChapterId { get; set; }

        [BsonElement("obtained")]
        public int[] Obtained { get; set; }
    }
}
