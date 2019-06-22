using MongoDB.Driver;
using StoryBot.Model;
using System;
using System.Collections.Generic;

namespace StoryBot.Logic
{
    public class DatabaseHandler
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IMongoCollection<StoryDocument> storiesCollection;

        private readonly IMongoCollection<SaveDocument> savesCollection;

        public DatabaseHandler(IMongoDatabase database)
        {
            storiesCollection = database.GetCollection<StoryDocument>("stories");
            savesCollection = database.GetCollection<SaveDocument>("saves");
        }

        // Stories //

        /// <summary>
        /// Returns chapter by story ID and chapter ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="chapter"></param>
        /// <returns></returns>
        public StoryDocument GetChapter(int id, int chapter)
        {
            var filter = Builders<StoryDocument>.Filter;
            return storiesCollection.Find(filter.Eq("id", id) & filter.Eq("chapter", chapter)).Single();
        }

        /// <summary>
        /// Returns all chapters of story by story ID
        /// </summary>
        /// <param name="storyId"></param>
        /// <returns></returns>
        public List<StoryDocument> GetAllChapters(int storyId)
        {
            return storiesCollection.Find(Builders<StoryDocument>.Filter.Eq("id", storyId)).SortBy(x => x.Chapter).ToList();
        }

        /// <summary>
        /// Returns prologue by story ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="chapter"></param>
        /// <returns></returns>
        public StoryDocument GetPrologue(int storyId)
        {
            return GetChapter(storyId, 0);
        }

        /// <summary>
        /// Returns all story prologues by its ID
        /// </summary>
        /// <returns></returns>
        public List<StoryDocument> GetAllPrologues()
        {
            return storiesCollection.Find(Builders<StoryDocument>.Filter.Eq("chapter", 0)).SortBy(x => x.Id).ToList();
        }

        // Saves //

        /// <summary>
        /// Returns save by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SaveDocument GetSave(long id)
        {
            var results = savesCollection.Find(Builders<SaveDocument>.Filter.Eq("id", id));
            try
            {
                return results.Single();
            }
            catch (InvalidOperationException)
            {
                if (results.CountDocuments() == 0)
                {
                    logger.Warn($"Save for {id} not found. Creating one...");
                    savesCollection.InsertOne(new SaveDocument(id));
                    return GetSave(id);
                }
                else throw;
            }
        }

        /// <summary>
        /// Updates save
        /// </summary>
        /// <param name="id"></param>
        /// <param name="save"></param>
        public void UpdateSave(SaveDocument save)
        {
            savesCollection.ReplaceOne(Builders<SaveDocument>.Filter.Eq("id", save.Id), save);
        }
    }
}
