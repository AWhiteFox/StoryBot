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

        public StoryDocument GetStory(int id, int chapter)
        {
            var filter = Builders<StoryDocument>.Filter;
            return collection.Find(filter.Eq("id", id) & filter.Eq("chapter", chapter)).Single();
        }

        public List<StoryDocument> GetAllStories()
        {
            return collection.Find(Builders<StoryDocument>.Filter.Eq("chapter", 0)).ToList();
        }

        public List<StoryDocument> GetStoryChapters(int storyId)
        {
            return collection.Find(Builders<StoryDocument>.Filter.Eq("id", storyId)).ToList();
        }
    }
}
