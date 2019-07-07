using System.Collections.Generic;
using StoryBot.Model;

namespace StoryBot.Abstractions
{
    public interface IStoriesHandler
    {
        List<StoryDocument> GetAllPrologues();
        StoryDocument GetEpisode(int storyId, int episodeId);
        List<StoryDocument> GetStoryEpisodes(int storyId);
    }
}