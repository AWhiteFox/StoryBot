using MongoDB.Driver;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StoryBot.Logic
{
    [Obsolete]
    public class StoriesHandler
    {
        private readonly IMongoCollection<StoryDocument> collection;

        public StoriesHandler(IMongoCollection<StoryDocument> _collection)
        {
            collection = _collection;
        }

        public StoryDocument GetStoryChapter(int id, int chapter)
        {
            var filter = Builders<StoryDocument>.Filter;
            return collection.Find(filter.Eq("id", id) & filter.Eq("chapter", chapter)).Single();
        }

        public List<StoryDocument> GetAllStoryChapters(int storyId)
        {
            return collection.Find(Builders<StoryDocument>.Filter.Eq("id", storyId)).SortBy(x => x.Chapter).ToList();
        }

        public StoryDocument GetPrologue(int storyId)
        {
            return GetStoryChapter(storyId, 0);
        }

        public List<StoryDocument> GetAllPrologues()
        {
            return collection.Find(Builders<StoryDocument>.Filter.Eq("chapter", 0)).SortBy(x => x.Id).ToList();
        }
    }
}
