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
                    logger.Info($"Save for {id} not found. Creating one...");
                    collection.InsertOne(new SaveDocument { Id = id });
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
        public void SaveProgress(long id, SaveProgress progress)
        {
            SaveDocument save = GetSave(id);
            save.Current = progress;
            UpdateSave(id, save);
        }

        /// <summary>
        /// Saves obtained ending
        /// </summary>
        /// <param name="id"></param>
        /// <param name="storyId"></param>
        /// <param name="ending"></param>
        public void SaveObtainedEnding(long id, int storyId, int chapterId, int ending)
        {
            SaveDocument save = GetSave(id);
            try
            {
                int questIndex = Array.FindIndex(save.Endings, x => x.StoryId == storyId);

                List<int> list = save.Endings[questIndex].Obtained.ToList();
                if (!list.Contains(ending)) list.Add(ending);
                list.Sort();
                save.Endings[questIndex].Obtained = list.ToArray();

                UpdateSave(id, save);
            }
            catch (Exception exception)
            {
                if (exception is ArgumentNullException || exception is IndexOutOfRangeException)
                {
                    if (save.Endings == null)
                    {
                        save.Endings = new SaveEndings[0];
                    }
                    List<SaveEndings> list = save.Endings.ToList();
                    list.Add(new SaveEndings { StoryId = storyId, ChapterId = chapterId, Obtained = new int[0] });
                    save.Endings = list.OrderBy(x => x.StoryId).ToArray();

                    UpdateSave(id, save);

                    SaveObtainedEnding(id, storyId, chapterId, ending);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Saves obtained achievement
        /// </summary>
        /// <param name="id"></param>
        /// <param name="storyId"></param>
        /// <param name="achievement"></param>
        public void SaveObtainedAchievement(long id, int storyId, int chapterId, int achievement)
        {
            SaveDocument save = GetSave(id);
            try
            {
                int questIndex = Array.FindIndex(save.Achievements, x => x.StoryId == storyId);

                List<int> list = save.Achievements[questIndex].Obtained.ToList();
                if (!list.Contains(achievement)) list.Add(achievement);
                list.Sort();
                save.Achievements[questIndex].Obtained = list.ToArray();

                UpdateSave(id, save);
            }
            catch (Exception exception)
            {
                if (exception is ArgumentNullException || exception is IndexOutOfRangeException)
                {
                    if (save.Achievements == null)
                    {
                        save.Achievements = new SaveAchievements[0];
                    }
                    List<SaveAchievements> list = save.Achievements.ToList();
                    list.Add(new SaveAchievements { StoryId = storyId, ChapterId = chapterId, Obtained = new int[0] });
                    save.Achievements = list.OrderBy(x => x.StoryId).ToArray();

                    UpdateSave(id, save);

                    SaveObtainedAchievement(id, storyId, chapterId, achievement);
                }
                else
                {
                    throw;
                }
            } 
        }

        // Update //

        /// <summary>
        /// Updates save
        /// </summary>
        /// <param name="id"></param>
        /// <param name="save"></param>
        private void UpdateSave(long id, SaveDocument save)
        {
            collection.ReplaceOne(Builders<SaveDocument>.Filter.Eq("id", id), save);
        }
    }
}
