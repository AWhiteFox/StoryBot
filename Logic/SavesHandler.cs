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

        #region Public methods

        public SaveProgress GetCurrent(long id)
        {
            return GetSave(id).Current;
        }

        public void SaveProgress(long id, SaveProgress progress)
        {
            SaveDocument save = GetSave(id);
            save.Current = progress;
            UpdateSave(id, save);
        }

        public void SaveObtainedEnding(long id, int storyId, int ending)
        {
            SaveDocument save = GetSave(id);
            try
            {
                int questIndex = Array.FindIndex(save.Endings, x => x.StoryId == storyId);

                List<int> list = save.Endings[questIndex].ObtainedEndings.ToList();
                if (!list.Contains(ending)) list.Add(ending);
                list.Sort();
                save.Endings[questIndex].ObtainedEndings = list.ToArray();

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
                    list.Add(new SaveEndings { StoryId = storyId, ObtainedEndings = new int[0] });
                    save.Endings = list.OrderBy(x => x.StoryId).ToArray();

                    UpdateSave(id, save);

                    SaveObtainedEnding(id, storyId, ending);
                }
                else
                {
                    throw;
                }
            }
        }

        public void SaveObtainedAchievement(long id, int storyId, int achievement)
        {
            SaveDocument save = GetSave(id);
            try
            {
                int questIndex = Array.FindIndex(save.Achievements, x => x.StoryId == storyId);

                List<int> list = save.Achievements[questIndex].ObtainedAchievements.ToList();
                if (!list.Contains(achievement)) list.Add(achievement);
                list.Sort();
                save.Achievements[questIndex].ObtainedAchievements = list.ToArray();

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
                    list.Add(new SaveAchievements { StoryId = storyId, ObtainedAchievements = new int[0] });
                    save.Achievements = list.OrderBy(x => x.StoryId).ToArray();

                    UpdateSave(id, save);

                    SaveObtainedAchievement(id, storyId, achievement);
                }
                else
                {
                    throw;
                }
            } 
        }

        #endregion

        #region Private methods

        private SaveDocument GetSave(long id)
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

        private void UpdateSave(long id, SaveDocument save)
        {
            collection.ReplaceOne(Builders<SaveDocument>.Filter.Eq("id", id), save);
        }

        #endregion
    }
}
