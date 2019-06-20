using MongoDB.Bson.Serialization.Attributes;

namespace StoryBot.Model
{
    public class SaveStoryStats
    {
        [BsonElement("id")]
        public int StoryId { get; set; }

        [BsonElement("chapters")]
        public SaveChapterStats[] Chapters { get; set; }

        public SaveStoryStats(int StoryId, SaveChapterStats[] Chapters)
        {
            this.StoryId = StoryId;
            this.Chapters = Chapters;
        }
    }
}
