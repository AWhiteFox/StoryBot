using MongoDB.Driver;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StoryBot.Messaging
{
    public class SavesHandler
    {
        private readonly IMongoCollection<SaveDocument> collection;

        public SavesHandler(IMongoCollection<SaveDocument> _collection)
        {
            collection =_collection;
        }

        public void SaveProgress(long id, Progress progress)
        {
            FilterDefinition<SaveDocument> filter = Builders<SaveDocument>.Filter.Eq("id", id);

            SaveDocument save = collection.Find(filter).Single();
            save.Current = progress;

            collection.ReplaceOne(filter, save);
        }

        public Progress GetProgress(long id)
        {
            return collection.Find(Builders<SaveDocument>.Filter.Eq("id", id)).Single().Current;
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
    }
}
