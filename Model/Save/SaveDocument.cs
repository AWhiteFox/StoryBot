using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Linq;

namespace StoryBot.Model
{
    public class SaveDocument
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static IMongoCollection<SaveDocument> collection;

        #region Bson Properties

        [BsonId]
        public ObjectId ObjectId { get; set; }

        [BsonElement("id")]
        public long Id { get; set; }

        [BsonElement("current")]
        public SaveProgress Current { get; set; }

        [BsonElement("stats")]
        public SaveStoryStats[] StoriesStats { get; set; }

        #endregion

        #region Constructor and Destructor

        public SaveDocument(long Id, SaveProgress Current = null, SaveStoryStats[] StoriesStats = null)
        {
            this.Id = Id;
            this.Current = Current ?? new SaveProgress();
            this.StoriesStats = StoriesStats ?? Array.Empty<SaveStoryStats>();
        }

        ~SaveDocument()
        {
            try
            {
                collection.ReplaceOne(Builders<SaveDocument>.Filter.Eq("id", Id), this);
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("SaveDocument collection doesn't set");
            }
        }

        #endregion

        public void AddEnding(int storyId, int chapterId, int endingId)
        {
            int statsId = Array.FindIndex(StoriesStats, x => x.StoryId == storyId);
            if (statsId == -1)
            {
                // TEMP: Log if chapter id != 0
                if (chapterId != 0)
                {
                    logger.Warn($"Can't find {chapterId + 1} chapters in save {Id} in story {storyId}");
                }
                StoriesStats.Append(new SaveStoryStats(storyId, new SaveChapterStats[chapterId + 1]));
                StoriesStats[0].Chapters[chapterId].ObtainedEndings.Append(endingId);
            }
            else
            {
                try
                {
                    StoriesStats[statsId].Chapters[chapterId].ObtainedEndings.Append(endingId);
                }
                catch (IndexOutOfRangeException)
                {
                    StoriesStats[statsId].Chapters.Append(new SaveChapterStats());
                    // TEMP: try
                    try
                    {
                        StoriesStats[statsId].Chapters[chapterId].ObtainedEndings.Append(endingId);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        logger.Error($"Error in chapters sequence in save {Id} in story {storyId}");
                        throw;
                    }
                }
            }
        }

        public void AddAchievement(int storyId, int chapterId, int achievementId)
        {
            int statsId = Array.FindIndex(StoriesStats, x => x.StoryId == storyId);
            if (statsId == -1)
            {
                // TEMP: Log if chapter id != 0
                if (chapterId != 0)
                {
                    logger.Warn($"Can't find {chapterId + 1} chapters in save {Id} in story {storyId}");
                }
                StoriesStats.Append(new SaveStoryStats(storyId, new SaveChapterStats[chapterId + 1]));
                StoriesStats[0].Chapters[chapterId].ObtainedAchievements.Append(achievementId);
            }
            else
            {
                try
                {
                    StoriesStats[statsId].Chapters[chapterId].ObtainedAchievements.Append(achievementId);
                }
                catch (IndexOutOfRangeException)
                {
                    StoriesStats[statsId].Chapters.Append(new SaveChapterStats());
                    // TEMP: try
                    try
                    {
                        StoriesStats[statsId].Chapters[chapterId].ObtainedAchievements.Append(achievementId);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        logger.Error($"Error in chapters sequence in save {Id} in story {storyId}");
                        throw;
                    }
                }
            }
        }
    }
}
