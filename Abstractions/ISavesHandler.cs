using StoryBot.Model;

namespace StoryBot.Abstractions
{
    public interface ISavesHandler
    {
        void CreateNew(SaveDocument save);
        SaveDocument Get(long id);
    }
}