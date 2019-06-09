﻿using Newtonsoft.Json;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace StoryBot.Logic
{
    public class MessagesHandler
    {
        private readonly IVkApi vkApi;

        private readonly StoriesHandler storiesHandler;

        private readonly SavesHandler savesHandler;

        public MessagesHandler(IVkApi _vkApi, StoriesHandler _storiesHandler, SavesHandler _savesHandler)
        {
            vkApi = _vkApi;
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
            stringBuilder.Append("Выберите историю:\n");

            SortedDictionary<int, StoryDocument> sorted = new SortedDictionary<int, StoryDocument>();
            foreach (StoryDocument story in stories)
            {
                sorted.Add(story.Id, story);
            }

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(true);
            foreach (KeyValuePair<int, StoryDocument> entry in sorted)
            {
                stringBuilder.Append($"[ {entry.Key + 1} ] {entry.Value.Name}\n");
                keyboardBuilder.AddButton(
                    $"[ {entry.Key + 1} ]",
                    System.Web.HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(new SaveProgress
                    {
                        Story = entry.Value.Id,
                        Storyline = entry.Value.Beginning,
                        Position = 0
                    })),
                    KeyboardButtonColor.Primary);
            }

            savesHandler.SaveProgress(peerId, new SaveProgress());

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString(),
                Keyboard = keyboardBuilder.Build()
            });
        }

        /// <summary>
        /// Sends an error message to user
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="content"></param>
        public void SendError(long peerId, string content)
        {
            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = $"Во время обработки вашего сообщения произошла непредвиденная ошибка:\n\n{content}\n\nПожалуйста сообщите администрации"
            });
        }

        /// <summary>
        /// Sends content and keyboard again
        /// </summary>
        /// <param name="peerId"></param>
        public void SendAgain(long peerId)
        {
            SendContent(peerId, savesHandler.GetCurrent(peerId));
        }

        /// <summary>
        /// Handles message if it is a number
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="number"></param>
        public void HandleNumber(long peerId, int number)
        {
            try
            {
                SaveProgress progress = savesHandler.GetCurrent(peerId);

                StoryDocument story;
                if (progress.Story != null)
                {
                    story = storiesHandler.GetStory((int)progress.Story);

                    StoryOption storyOption = Array
                        .Find(story.Story, x => x.Tag == (progress.Storyline ?? story.Beginning))
                        .Elements[progress.Position]
                        .Options[number];

                    progress.Storyline = storyOption.Storyline ?? progress.Storyline;
                    progress.Position = storyOption.Position;
                }
                else
                {
                    story = storiesHandler.GetStory(number);

                    progress.Story = number;
                    progress.Storyline = story.Beginning;
                    progress.Position = 0;
                }
                SendContent(peerId, progress, story);
            }
            catch (IndexOutOfRangeException)
            {
                vkApi.Messages.Send(new MessagesSendParams
                {
                    RandomId = new DateTime().Millisecond,
                    PeerId = peerId,
                    Message = "Выберите вариант из представленных"
                });
            }
        }
        
        /// <summary>
        /// Handles message with keyboard payload
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="payload"></param>
        public void HandleKeyboard(long peerId, string payload)
        {
            SendContent(peerId, JsonConvert.DeserializeObject<SaveProgress>(payload));
        }

        /// <summary>
        /// Gets date of last message in conversat
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        public DateTime? GetLastMessageDate(long peerId)
        {
            try
            {
                return vkApi.Messages.GetHistory(new MessagesGetHistoryParams { Count = 1, PeerId = peerId }).Messages.ToCollection()[0].Date;
            }
            catch (VkNet.Exception.ParameterMissingOrInvalidException)
            {
                return DateTime.MinValue;
            }
        }

        #region Private methods

        /// <summary>
        /// Sends basic message with content and options
        /// </summary>
        /// <param name="progress"></param>
        private void SendContent(long peerId, SaveProgress progress, StoryDocument story = null)
        {
            story = story ?? storiesHandler.GetStory((int)progress.Story);

            if (progress.Storyline != "Ending")
            {
                savesHandler.SaveProgress(peerId, progress);

                StorylineElement storylineElement = Array.Find(story.Story, x => x.Tag == progress.Storyline).Elements[progress.Position];

                StringBuilder stringBuilder = new StringBuilder();
                if (progress.Achievement != null)
                {
                    var achievement = story.Achievements[(int)progress.Achievement];
                    stringBuilder.Append($"Вы заработали достижение {achievement.Name}!\n - {achievement.Description}\n\n");
                    savesHandler.SaveObtainedAchievement(peerId, story.Id, (int)progress.Achievement);
                }
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
                        System.Web.HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(new SaveProgress
                        {
                            Story = progress.Story,
                            Storyline = x.Storyline ?? progress.Storyline,
                            Position = x.Position,
                            Achievement = x.Achievement
                        })),
                        KeyboardButtonColor.Default);
                }
                
                vkApi.Messages.Send(new MessagesSendParams
                {
                    RandomId = new DateTime().Millisecond,
                    PeerId = peerId,
                    Message = stringBuilder.ToString(),
                    Keyboard = keyboardBuilder.Build()
                });               
            }
            else
            {
                savesHandler.SaveObtainedEnding(peerId, story.Id, progress.Position);
                SendEnding(peerId, story.Endings[progress.Position], story.Endings.Length - 1);
            }
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

            stringBuilder.Append("\nТеперь вы можете пройти её еще раз или выбрать другую");

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString()
            });
            SendMenu(peerId);
        }

        #endregion
    }
}