using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace StoryBot.Model
{
    public class SaveEpisodeStats
    {
        [BsonElement("endings")]
        public List<int> ObtainedEndings { get; set; }

        [BsonElement("achievements")]
        public List<int> ObtainedAchievements { get; set; }

        public SaveEpisodeStats(List<int> ObtainedEndings = null, List<int> ObtainedAchievements = null)
        {
            this.ObtainedEndings = ObtainedEndings ?? new List<int>();
            this.ObtainedAchievements = ObtainedAchievements ?? new List<int>();
        }
    }
}
