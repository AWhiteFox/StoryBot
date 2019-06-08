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

        public StoryDocument GetStory(string tag)
        {
            return collection.Find(Builders<StoryDocument>.Filter.Eq("tag", tag)).Single();
        }

        public List<StoryDocument> GetAllStories()
        {
            return collection.Find(Builders<StoryDocument>.Filter.Empty).ToList();
        }
    }
}
