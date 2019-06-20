using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace StoryBot.Model
{
    public class SaveDocument
    {
        [BsonId]
        public ObjectId ObjectId { get; set; }

        [BsonElement("id")]
        public long Id { get; set; }

        [BsonElement("current")]
        public SaveProgress Current { get; set; }

        [BsonElement("stats")]
        public SaveStoryStats[] StoriesStats { get; set; }

        public SaveDocument(long Id, SaveProgress Current = null, SaveStoryStats[] StoriesStats = null)
        {
            this.Id = Id;
            this.Current = Current;
            this.StoriesStats = StoriesStats ?? Array.Empty<SaveStoryStats>();
        }
    }
}
