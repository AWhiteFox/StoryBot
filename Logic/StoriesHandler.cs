using MongoDB.Driver;
using StoryBot.Model;
using System.Collections.Generic;

namespace StoryBot.Logic
{
    public class StoriesHandler
    {
        private readonly IMongoCollection<StoryDocument> collection;

        public StoriesHandler(IMongoCollection<StoryDocument> _collection)
        {
            collection = _collection;
        }

        public StoryDocument GetStory(int id)
        {
            return collection.Find(Builders<StoryDocument>.Filter.Eq("id", id)).Single();
        }

        public List<StoryDocument> GetAllStories()
        {
            return collection.Find(Builders<StoryDocument>.Filter.Empty).ToList();
        }
    }
}
