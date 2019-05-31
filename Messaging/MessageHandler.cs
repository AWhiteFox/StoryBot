using MongoDB.Driver;
using System.Collections.Generic;
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
        // REFACTOR TO NO ref

        public IMongoDatabase database;

        public MessageHandler(IMongoDatabase _database)
        {
            database = _database;
        }

        /// <summary>
        /// Sends a quest choosing dialog
        /// </summary>
        /// <param name="response"></param>
        public void SendMenu(ref MessagesSendParams response)
        {
            // Get all stories
            var results = database.GetCollection<StoryDocument>("stories").Find(Builders<StoryDocument>.Filter.Empty).ToList();

            // Generate keyboard
            KeyboardBuilder keyboard = new KeyboardBuilder();
            keyboard.SetOneTime();
            foreach (StoryDocument x in results)
            {
                keyboard.AddButton(x.name, x.tag, KeyboardButtonColor.Primary);
            }

            // Generate response
            response.Message = "Выберите квест";
            response.Keyboard = keyboard.Build();
        }

        /// <summary>
        /// Handles message with keyboard payload
        /// </summary>
        /// <param name="response"></param>
        /// <param name="_payload"></param>
        /// <param name="author_id"></param>
        public void HandleKeyboard(ref MessagesSendParams response, string _payload)
        {
            // Deserialize payload and find story in DB
            StoryProgress payload = StoryProgressConvert.Deserialize(_payload);
            StoryDocument story = database.GetCollection<StoryDocument>("stories").Find(Builders<StoryDocument>.Filter.Eq("tag", payload.story)).ToList()[0];

            // If no storyline provided set storyline to beginning and position to 0
            if (string.IsNullOrEmpty(payload.storyline))
            {
                payload.storyline = story.beginning;
                payload.position = 0;
            }
            // Check if provided storyline is an ending
            else if (payload.storyline == "Endings")
            {
                //dynamic ending = ((dynamic)((IDictionary<string, object>)story.story)[payload.storyline])[payload.position];
                return;
            }

            // Get storyline element
            dynamic storylineElement = ((dynamic)((IDictionary<string, object>)story.story)[payload.storyline])[payload.position];

            // Generate response message
            StringBuilder str = new StringBuilder(); 
            foreach (string x in storylineElement.content)
            {
                str.Append(x);
            }
            response.Message = str.ToString();

            // Generate response keyboard
            KeyboardBuilder keyboard = new KeyboardBuilder();
            keyboard.SetOneTime();
            foreach (var x in storylineElement.options)
            {
                string next;
                bool isANumber = int.TryParse(x.next, out int position);
                // If provided NEXT isn't a number split it to Storyline and Position
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
                // Else set Storyline to current
                else
                {
                    next = StoryProgressConvert.Serialize(new StoryProgress
                    {
                        story = payload.story,
                        storyline = payload.storyline,
                        position = position
                    });
                }
                keyboard.AddButton(x.content, next, KeyboardButtonColor.Default);
            }           
            response.Keyboard = keyboard.Build();
        }
    }  
}
