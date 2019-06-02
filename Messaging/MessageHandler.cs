using MongoDB.Driver;
using System;
using System.Linq;
using System.Text;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using static StoryBot.Messaging.DatabaseObjects;

namespace StoryBot.Messaging
{
    public class MessageHandler
    {
        #region Private members and constructor

        private readonly IVkApi vkApi;

        private readonly IMongoDatabase database;

        public MessageHandler(IVkApi _api, IMongoDatabase _database)
        {
            vkApi = _api;
            database = _database;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends "Hello, world!"
        /// </summary>
        /// <param name="peerId"></param>
        public void SendHelloWorld(long peerId)
        {
            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = "Hello, world!"
            });
        }

        /// <summary>
        /// Sends a quest choosing dialog
        /// </summary>
        /// <param name="peerId"></param>
        public void SendMenu(long peerId)
        {
            var results = database.GetCollection<StoryDocument>("stories").Find(Builders<StoryDocument>.Filter.Empty).ToList();

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(true);
            foreach (StoryDocument x in results)
            {
                keyboardBuilder.AddButton(x.Name, x.Tag, KeyboardButtonColor.Primary);
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = "Выберите историю:",
                Keyboard = keyboardBuilder.Build()
            });
        }

        /// <summary>
        /// Handles message with keyboard payload
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="_payload"></param>
        public void HandleKeyboard(long peerId, string _payload)
        {
            StoryProgress payload = StoryProgressConvert.Deserialize(_payload);
            StoryDocument story = database.GetCollection<StoryDocument>("stories").Find(Builders<StoryDocument>.Filter.Eq("tag", payload.Story)).Single();

            if (string.IsNullOrEmpty(payload.Storyline))
            {
                payload.Storyline = story.Beginning;
                payload.Position = 0;
            }
            else if (payload.Storyline == "Ending")
            {
                SendEnding(peerId, story.Endings[payload.Position], story.Endings.Length - 1);
                return;
            }

            StorylineElement storylineElement = Array.Find(story.Story, x => x.Tag == payload.Storyline).Elements[payload.Position];

            StringBuilder stringBuilder = new StringBuilder();
            foreach (string x in storylineElement.Content)
            {
                stringBuilder.Append(x + "\n");
            }

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(true);
            foreach (StoryOption x in storylineElement.Options)
            {
                string next = StoryProgressConvert.Serialize(new StoryProgress
                {
                    Story = payload.Story,
                    Storyline = x.Next ?? payload.Storyline,
                    Position = x.NextPosition
                });
                keyboardBuilder.AddButton(x.Content, next, KeyboardButtonColor.Default);
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString(),
                Keyboard = keyboardBuilder.Build()
            });
        }

        private void SendEnding(long peerId, Ending ending, int alternativeEndingsCount)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string x in ending.Content)
            {
                stringBuilder.Append(x + "\n");
            }

            if (ending.Type == 0)
            {
                stringBuilder.Append($"Поздравляем, вы получили каноничную концовку \"{ending.Name}\"!\n");
                stringBuilder.Append($"Эта история содержит еще {alternativeEndingsCount} альтернативные концовки.");
            }
            else
            {
                stringBuilder.Append($"Поздравляем, вы получили альтернативную концовку \"{ending.Name}\"!\n");
                stringBuilder.Append($"Эта история содержит еще {alternativeEndingsCount - 1} альтернативные концовки и одну каноничную.");
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString(),
            });
            SendMenu(peerId);
        }

        #endregion
    }
}