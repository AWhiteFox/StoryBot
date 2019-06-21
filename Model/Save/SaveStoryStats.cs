using MongoDB.Bson.Serialization.Attributes;
using System;

namespace StoryBot.Model
{
    public class SaveStoryStats
    {
        [BsonElement("id")]
        public int StoryId { get; set; }

        [BsonElement("chapters")]
        public SaveChapterStats[] Chapters { get; set; }

        public SaveStoryStats(int StoryId, SaveChapterStats[] Chapters = null)
        {
            this.StoryId = StoryId;
            this.Chapters = Chapters ?? Array.Empty<SaveChapterStats>();
        }
    }
}
