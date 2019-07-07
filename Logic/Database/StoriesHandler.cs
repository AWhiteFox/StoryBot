using MongoDB.Driver;
using StoryBot.Abstractions;
using StoryBot.Model;
using System.Collections.Generic;
using System.Linq;

namespace StoryBot.Logic
{
    public class StoriesHandler : IStoriesHandler
    {
        private readonly IMongoCollection<StoryDocument> collection;

        public StoriesHandler(IMongoCollection<StoryDocument> collection)
        {
            this.collection = collection;
        }

        /// <summary>
        /// Returns episode by story ID and episode ID
        /// </summary>
        /// <param name="storyId"></param>
        /// <param name="episodeId"></param>
        /// <returns></returns>
        public StoryDocument GetEpisode(int storyId, int episodeId)
        {
            var filter = Builders<StoryDocument>.Filter;
            return collection.Find(filter.Eq("id", storyId) & filter.Eq("episode", episodeId)).Single();
        }

        /// <summary>
        /// Returns all episodes of story by story ID
        /// </summary>
        /// <param name="storyId"></param>
        /// <returns></returns>
        public List<StoryDocument> GetStoryEpisodes(int storyId)
        {
            return collection.Find(Builders<StoryDocument>.Filter.Eq("id", storyId)).SortBy(x => x.Episode).ToList();
        }

        /// <summary>
        /// Returns all story prologues by its ID
        /// </summary>
        /// <returns></returns>
        public List<StoryDocument> GetAllPrologues()
        {
            return collection.Find(Builders<StoryDocument>.Filter.Eq("episode", 0)).SortBy(x => x.StoryId).ToList();
        }
    }
}
