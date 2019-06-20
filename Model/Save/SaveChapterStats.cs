using MongoDB.Bson.Serialization.Attributes;
using System;

namespace StoryBot.Model
{
    public class SaveChapterStats
    {
        [BsonElement("endings")]
        public int[] ObtainedEndings { get; set; }

        [BsonElement("achievements")]
        public int[] ObtainedAchievements { get; set; }

        public SaveChapterStats(int[] ObtainedEndings = null, int[] ObtainedAchievements = null)
        {
            this.ObtainedEndings = ObtainedEndings ?? Array.Empty<int>();
            this.ObtainedAchievements = ObtainedAchievements ?? Array.Empty<int>();
        }
    }
}
