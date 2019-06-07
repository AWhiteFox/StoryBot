using Newtonsoft.Json;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace StoryBot.Messaging
{
    public class MessageHandler
    {
        private readonly IVkApi vkApi;

        private readonly StoriesHandler storiesHandler;

        private readonly SavesHandler savesHandler;

        public MessageHandler(IVkApi _api, StoriesHandler _storiesHandler, SavesHandler _savesHandler)
        {
            vkApi = _api;
            storiesHandler = _storiesHandler;
            savesHandler = _savesHandler;
        }

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
            List<StoryDocument> stories = storiesHandler.GetAllStories();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Выберите историю:");
            stringBuilder.Append("---------------------");

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(true);
            for (int i = 0; i < stories.Count; i++)
            {
                var x = stories[i];
                stringBuilder.Append($"[ {i + 1} ] {x.Name}");
                keyboardBuilder.AddButton($"[ {i + 1} ]", x.Tag, KeyboardButtonColor.Primary);
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString(),
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
            Progress payload = JsonConvert.DeserializeObject<Progress>(_payload);
            StoryDocument story = storiesHandler.GetStory(payload.Story);

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
            stringBuilder.Append("\n");

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(true);
            for (int i = 0; i < storylineElement.Options.Length; i++)
            {
                var x = storylineElement.Options[i];

                stringBuilder.Append($"[ {i + 1} ] {x.Content}\n");

                keyboardBuilder.AddButton($"[ {i + 1} ]",
                    JsonConvert.SerializeObject(new Progress
                    {
                        Story = payload.Story,
                        Storyline = x.Next ?? payload.Storyline,
                        Position = x.NextPosition
                    }),
                    KeyboardButtonColor.Default);
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString(),
                Keyboard = keyboardBuilder.Build()
            });
            savesHandler.SaveProgress(peerId, payload);
        }

        /// <summary>
        /// Sends an ending message
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="ending"></param>
        /// <param name="alternativeEndingsCount"></param>
        private void SendEnding(long peerId, StoryEnding ending, int alternativeEndingsCount)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string x in ending.Content)
            {
                stringBuilder.Append(x + "\n");
            }

            if (ending.Type == 0)
            {
                stringBuilder.Append($"\nПоздравляем, вы получили каноничную концовку \"{ending.Name}\"!\n\n");
                stringBuilder.Append($"Эта история содержит еще {alternativeEndingsCount} альтернативные концовки.");
            }
            else
            {
                stringBuilder.Append($"\nПоздравляем, вы получили альтернативную концовку \"{ending.Name}\"!\n\n");
                stringBuilder.Append($"Эта история содержит еще {alternativeEndingsCount - 1} альтернативные концовки и одну каноничную.");
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString()
            });
            SendMenu(peerId);
        }

        public DateTime? GetLastMessageDate(long peerId)
        {
            return vkApi.Messages.GetHistory(new MessagesGetHistoryParams { Count = 1, PeerId = peerId }).Messages.ToCollection()[0].Date;
        }
    }
}