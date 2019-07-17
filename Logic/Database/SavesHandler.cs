using MongoDB.Driver;
using StoryBot.Abstractions;
using StoryBot.Model;
using System;
using System.Linq;

namespace StoryBot.Logic
{
    public class SavesHandler : ISavesHandler
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IMongoCollection<SaveDocument> collection;

        public SavesHandler(IMongoCollection<SaveDocument> collection)
        {
            this.collection = collection;
        }

        /// <summary>
        /// Returns save by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SaveDocument Get(long id)
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
                    return Get(id);
                }
                else throw;
            }
        }

        /// <summary>
        /// Inserts SaveDocument to saves collection
        /// </summary>
        /// <param name="save"></param>
        public void CreateNew(SaveDocument save)
        {
            collection.InsertOne(save);
        }

        public void Update(SaveDocument save)
        {
            collection.ReplaceOne(Builders<SaveDocument>.Filter.Eq("id", save.Id), save);
        }
    }
}
