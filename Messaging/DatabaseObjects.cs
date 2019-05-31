using MongoDB.Bson;

namespace StoryBot.Messaging
{
    public static class DatabaseObjects
    {
        public class StoryDocument
        {
            public ObjectId _id { get; set; }
            public string tag { get; set; }
            public string name { get; set; }
            public object story { get; set; }
            public string beginning { get; set; }
            public object[] endings { get; set; }
        }

        public class StorylineElement
        {
            public string[] content { get; set; }
            public StoryOption[] options { get; set; }
        }
        
        public class StoryOption
        {
            public string content { get; set; }
            public string next { get; set; }
        }

        public class SaveDocument
        {
            public ObjectId _id { get; set; }
            public long? id { get; set; }
            public StoryProgress current { get; set; }
            public object endings { get; set; }
        }

        public class StoryProgress
        {
            public string story { get; set; }
            public string storyline { get; set; }
            public int position { get; set; }
        }
    }
}
