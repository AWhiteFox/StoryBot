using MongoDB.Driver;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StoryBot.Messaging
{
    public class SavesHandler
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IMongoCollection<SaveDocument> collection;

        public SavesHandler(IMongoCollection<SaveDocument> _collection)
        {
            collection =_collection;
        }

        public void SaveProgress(long id, Progress progress)
        {
            FilterDefinition<SaveDocument> filter = Builders<SaveDocument>.Filter.Eq("id", id);
            var results = collection.Find(filter);

            try
            {
                SaveDocument save = results.Single();
                save.Current = progress;

                collection.ReplaceOne(filter, save);
            }
            catch (InvalidOperationException)
            {
                logger.Info($"Save for {id} not found. Creating one...");
                CreateSave(id);
                SaveProgress(id, progress);
            }
        }

        public Progress GetProgress(long id)
        {
            var results = collection.Find(Builders<SaveDocument>.Filter.Eq("id", id));
            try
            {
                return results.Single().Current;
            }
            catch (InvalidOperationException)
            {
                if (results.CountDocuments() == 0)
                {
                    logger.Info($"Save for {id} not found. Creating one...");
                    CreateSave(id);
                    return GetProgress(id);
                }
                else throw;
            }
        }

        public void SaveObtainedEnding(long id, string questTag, int ending)
        {
            FilterDefinition<SaveDocument> filter = Builders<SaveDocument>.Filter.Eq("id", id);

            SaveDocument save = collection.Find(filter).Single();
            int questIndex = Array.FindIndex(save.Endings, x => x.QuestTag == questTag);

            List<int> list = new List<int>();
            list.AddRange(save.Endings[questIndex].ObtainedEndings);
            if (!list.Contains(ending)) list.Add(ending);
            list.Sort();
            save.Endings[questIndex].ObtainedEndings = list.ToArray();

            collection.ReplaceOne(filter, save);
        }

        private void CreateSave(long id)
        {
            collection.InsertOne(new SaveDocument
            {
                Id = id,
                Current = null,
                Endings = null
            });
        }
    }
}
