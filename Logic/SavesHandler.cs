using MongoDB.Driver;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StoryBot.Logic
{
    public class SavesHandler
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IMongoCollection<SaveDocument> collection;

        public SavesHandler(IMongoCollection<SaveDocument> _collection)
        {
            collection =_collection;
        }

        // Get //

        /// <summary>
        /// Returns save by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SaveDocument GetSave(long id)
        {
            var results = collection.Find(Builders<SaveDocument>.Filter.Eq("id", id));
            try
            {
                return results.Single();
            }
            catch (InvalidOperationException)
            {
                if (results.CountDocuments() == 0)
                {
                    logger.Warn($"Save for {id} not found. Creating one...");
                    collection.InsertOne(new SaveDocument(id));
                    return GetSave(id);
                }
                else throw;
            }
        }

        // Save //

        /// <summary>
        /// Saves progress
        /// </summary>
        /// <param name="id"></param>
        /// <param name="progress"></param>
        [Obsolete]
        public void SaveProgress(long id, SaveProgress progress)
        {
            SaveDocument save = GetSave(id);
            save.Current = progress;
            UpdateSave(save);
        }

        /// <summary>
        /// Saves obtained ending
        /// </summary>
        /// <param name="id"></param>
        /// <param name="storyId"></param>
        /// <param name="ending"></param>
        [Obsolete]
        public void SaveObtainedEnding(long id, int storyId, int chapterId, int ending)
        {
            SaveDocument save = GetSave(id);
            save.AddEnding(storyId, chapterId, ending);
            UpdateSave(save);
        }

        /// <summary>
        /// Saves obtained achievement
        /// </summary>
        /// <param name="id"></param>
        /// <param name="storyId"></param>
        /// <param name="achievement"></param>
        [Obsolete]
        public void SaveObtainedAchievement(long id, int storyId, int chapterId, int achievement)
        {
            SaveDocument save = GetSave(id);
            save.AddAchievement(storyId, chapterId, achievement);
            UpdateSave(save);
        }

        // Update //

        /// <summary>
        /// Updates save
        /// </summary>
        /// <param name="id"></param>
        /// <param name="save"></param>
        [Obsolete]
        private void UpdateSave(SaveDocument save)
        {
            collection.ReplaceOne(Builders<SaveDocument>.Filter.Eq("id", save.Id), save);
        }
    }
}
