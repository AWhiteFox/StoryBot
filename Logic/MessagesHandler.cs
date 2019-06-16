using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Text;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace StoryBot.Logic
{
    public class MessagesHandler
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
        /// Handles new message
        /// </summary>
        /// <param name="messageObject"></param>
        public void HandleNew(JObject messageObject)
        {
            var content = Message.FromJson(new VkResponse(messageObject));
            long peerId = content.PeerId.Value;

            try
            {
                // Make sure it is the last message
                if (vkApi.Messages.GetHistory(new MessagesGetHistoryParams { Count = 1, PeerId = peerId }).Messages.ToCollection()[0].Date <= content.Date)
                {
                    if (content.Text[0] == '!')
                    {
                        switch (content.Text.Remove(0, 1).ToLower())
                        {
                            case "helloworld":
                                SendHelloWorld(peerId);
                                return;
                            case "reset":
                                SendStoryChoiceDialog(peerId);
                                return;
                            case "repeat":
                                SendContent(peerId, savesHandler.GetCurrent(peerId)); ;
                                return;
                        }
                    }
                    else if (content.Payload != null)
                    {
                        if (int.TryParse(content.Text, out int number))
                        {
                            //Handle payload command
                        }
                        else
                        {
                            HandleKeyboard(peerId, JsonConvert.DeserializeObject<Payload>(content.Payload).Button);
                        }
                    }
                    else if (int.TryParse(content.Text, out int number))
                    {
                        HandleNumber(peerId, number - 1);
                    }
                }
                else
                {
                    logger.Info($"Ignoring old message ({content.Date.ToString()}) from {content.PeerId}");
                }
            }
            catch (Exception exception)
            {
                vkApi.Messages.Send(new MessagesSendParams
                {
                    RandomId = new DateTime().Millisecond,
                    PeerId = peerId,
                    Message = $"Во время обработки вашего сообщения произошла непредвиденная ошибка:\n\n{exception.Message}\n\nПожалуйста сообщите администрации"
                });
                throw;
            }
        }
        
        // Senders //

        /// <summary>
        /// Sends "Hello, world!"
        /// </summary>
        /// <param name="peerId"></param>
        private void SendHelloWorld(long peerId)
        {
            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = "Hello, world!"
            });
        }

        /// <summary>
        /// Sends basic message with content and options
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="progress"></param>
        /// <param name="story"></param>
        private void SendContent(long peerId, SaveProgress progress, StoryDocument story = null)
        {
            story = story ?? storiesHandler.GetStory((int)progress.Story, (int)progress.Chapter);

            if (progress.Storyline != "Ending")
            {
                savesHandler.SaveProgress(peerId, progress);

                StorylineElement storylineElement = Array.Find(story.Storylines, x => x.Tag == progress.Storyline).Elements[progress.Position];

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
                            Chapter = progress.Chapter,
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
            SendStoryChoiceDialog(peerId);
        }

        // Dialogs //

        /// <summary>
        /// Sends a quest choosing dialog
        /// </summary>
        /// <param name="peerId"></param>
        private void SendStoryChoiceDialog(long peerId)
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
                        Story = entry.Value.Id
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
        /// Sends a chapter choosing dialog
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="storyId"></param>
        private void SendChapterChoiceDialog(long peerId, int storyId)
        {
            List<StoryDocument> stories = storiesHandler.GetAllStories();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Выберите главу:\n");

            SortedDictionary<int, StoryDocument> sorted = new SortedDictionary<int, StoryDocument>();
            foreach (StoryDocument story in stories)
            {
                sorted.Add(story.Chapter, story);
            }

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(true);
            foreach (KeyValuePair<int, StoryDocument> entry in sorted)
            {
                stringBuilder.Append($"Глава {entry.Key + 1}. {entry.Value.ChapterName}\n");
                keyboardBuilder.AddButton(
                    $"[ {entry.Key + 1} ]",
                    System.Web.HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(new SaveProgress
                    {
                        Story = storyId,
                        Chapter = entry.Value.Chapter,
                        Storyline = entry.Value.Beginning,
                        Position = 0
                    })),
                    KeyboardButtonColor.Primary);
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString(),
                Keyboard = keyboardBuilder.Build()
            });
        }

        // Handles //

        /// <summary>
        /// Handles message if it is a number
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="number"></param>
        private void HandleNumber(long peerId, int number)
        {
            try
            {
                SaveProgress progress = savesHandler.GetCurrent(peerId);

                StoryDocument story;
                if (progress.Story != null)
                {
                    if (progress.Chapter != null)
                    {
                        story = storiesHandler.GetStory((int)progress.Story, (int)progress.Chapter);

                        StoryOption storyOption = Array
                            .Find(story.Storylines, x => x.Tag == (progress.Storyline ?? story.Beginning))
                            .Elements[progress.Position]
                            .Options[number];

                        progress.Storyline = storyOption.Storyline ?? progress.Storyline;
                        progress.Position = storyOption.Position;
                        progress.Achievement = storyOption.Achievement;
                    }
                    else
                    {
                        story = storiesHandler.GetStory((int)progress.Story, number);

                        progress.Chapter = number;
                        progress.Storyline = story.Beginning;
                        progress.Position = 0;
                    }
                }
                else
                {
                    SendChapterChoiceDialog(peerId, number);
                    return;
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
        /// <param name="_payload"></param>
        private void HandleKeyboard(long peerId, string _payload)
        {
            SaveProgress payload = JsonConvert.DeserializeObject<SaveProgress>(_payload);

            if (payload.Chapter != null)
            {
                SendContent(peerId, payload);
            }
            else
            {
                SendChapterChoiceDialog(peerId, (int)payload.Story);
            } 
        }
    }
}