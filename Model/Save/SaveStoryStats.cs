using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace StoryBot.Model
{
    public class SaveStoryStats
    {
        [BsonElement("id")]
        public int StoryId { get; set; }

        [BsonElement("episodes")]
        public List<SaveEpisodeStats> Episodes { get; set; }

        public SaveStoryStats(int StoryId, List<SaveEpisodeStats> Episodes = null)
        {
            this.StoryId = StoryId;
            this.Episodes = Episodes ?? new List<SaveEpisodeStats>();
        }
    }
}
