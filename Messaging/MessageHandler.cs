using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using static StoryBot.Messaging.DatabaseObjects;

namespace StoryBot.Messaging
{
    public class MessageHandler
    {
        public IMongoDatabase database;

        public MessageHandler(IMongoDatabase _database)
        {
            database = _database;
        }

        public void SendMenu(ref MessagesSendParams response)
        {
            var stories = database.GetCollection<StoryDocument>("stories");
            var results = stories.Find(Builders<StoryDocument>.Filter.Empty).ToList();

            KeyboardBuilder keyboard = new KeyboardBuilder();
            keyboard.SetOneTime();

            foreach (StoryDocument x in results)
            {
                keyboard.AddButton(x.name, x.tag, KeyboardButtonColor.Primary);
            }

            response.Message = "Выберите квест";
            response.Keyboard = keyboard.Build();
        }

        public void HandleKeyboard(ref MessagesSendParams response, string _payload, long? author_id)
        {
            StoryProgress payload = StoryProgressConvert.Deserialize(_payload);
            StoryDocument story = database.GetCollection<StoryDocument>("stories").Find(Builders<StoryDocument>.Filter.Eq("tag", payload.story)).ToList()[0];

            //try
            //{
            //    var filter = Builders<SaveDocument>.Filter.Eq("id", author_id);
            //    var save = collection.Find(filter).ToList()[0];
            //}
            //catch (System.ArgumentOutOfRangeException)
            //{
            //    collection.InsertOne(new SaveDocument
            //    {
            //        id = author_id,
            //        current = new StoryProgress
            //        {
            //            story = payload.story,
            //            storyline = 
            //        }
            //    });
            //}

            string _storyline;
            if (!string.IsNullOrEmpty(payload.storyline))
            {
                _storyline = payload.storyline;
            }
            else
            {
                _storyline = story.beginning;
            }

            //FIX
            dynamic storylineElement = ((dynamic)((IDictionary<string, object>)story.story)[_storyline])[payload.position];
            //dynamic storylineElement = storyline[payload.position];

            StringBuilder str = new StringBuilder(); 
            foreach (string x in storylineElement.content)
            {
                str.Append(x);
            }

            KeyboardBuilder keyboard = new KeyboardBuilder();
            foreach (var x in storylineElement.options)
            {
                string next;
                int position;
                bool isANumber = int.TryParse(x.next, out position);
                if (!isANumber)
                {
                    string[] splitted = x.next.Split('.');
                    next = StoryProgressConvert.Serialize(new StoryProgress
                    {
                        story = payload.story,
                        storyline = splitted[0],
                        position = int.Parse(splitted[1])
                    });
                }
                else
                {
                    next = StoryProgressConvert.Serialize(new StoryProgress
                    {
                        story = payload.story,
                        storyline = _storyline,
                        position = position
                    });
                }
                keyboard.AddButton(x.content, next, KeyboardButtonColor.Default);
            }

            response.Message = str.ToString();
            response.Keyboard = keyboard.Build();
        }
    }  
}
