﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
                        string[] command = content.Text.Remove(0, 1).ToLower().Split(" ");
                        HandleCommand(peerId, command[0], command.Skip(1).ToArray());
                    }
                    else if (content.Payload != null)
                    {
                        if (content.Payload[0] == '!')
                        {
                            string[] command = content.Payload.Remove(0, 1).ToLower().Split(" ");
                            HandleCommand(peerId, command[0], command.Skip(1).ToArray());
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
            if (story == null)
            {
                story = storiesHandler.GetStoryChapter((int)progress.Story, (int)progress.Chapter);
            }  

            if (progress.Storyline != "Ending")
            {
                savesHandler.SaveProgress(peerId, progress);

                StorylineElement storylineElement = Array.Find(story.Storylines, x => x.Tag == progress.Storyline).Elements[progress.Position];

                StringBuilder stringBuilder = new StringBuilder();
                if (progress.Achievement != null)
                {
                    var achievement = story.Achievements[(int)progress.Achievement];
                    stringBuilder.Append($"Вы заработали достижение {achievement.Name}!\n - {achievement.Description}\n\n");
                    savesHandler.SaveObtainedAchievement(peerId, story.Id, story.Chapter, (int)progress.Achievement);
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
                savesHandler.SaveObtainedEnding(peerId, story.Id, story.Chapter, progress.Position);
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

        /// <summary>
        /// Sends story choice for stats dialog
        /// </summary>
        /// <param name="peerId"></param>
        private void SendStats(long peerId)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Общая статистика:\n\n");

            var save = savesHandler.GetSave(peerId);
            foreach (var s in storiesHandler.GetAllStories())
            {
                int completedChapters;
                try
                {
                    completedChapters = Array.Find(save.StoriesStats, x => x.StoryId == s.Id).Chapters.Length;
                }
                catch (NullReferenceException)
                {
                    completedChapters = 0;
                }
                stringBuilder.Append($"- {s.Name}: {completedChapters}/{storiesHandler.GetAllStoryChapters(s.Id).Count}\n");
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString()
            });
        }

        private void SendStats(long peerId, int storyId)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"Статистика по \"{storiesHandler.GetStoryName(storyId)}\":\n");

            SaveChapterStats[] chapters;
            try
            {
                chapters = Array.Find(savesHandler.GetSave(peerId).StoriesStats, x => x.StoryId == storyId).Chapters;
                for (int i = 0; i < chapters.Length; i++)
                {
                    var chapter = storiesHandler.GetStoryChapter(storyId, i);
                    stringBuilder.Append($"- {i + 1}: {chapters[i].ObtainedEndings}/{chapter.Endings.Length}, {chapters[i].ObtainedAchievements}/{chapter.Achievements.Length}\n");
                }
            }
            catch (NullReferenceException)
            {
                stringBuilder.Append("- Нет данных.");
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString()
            });
        }

        private void SendStats(long peerId, int storyId, int chapterId)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"Статистика по главе {chapterId + 1} истории \"{storiesHandler.GetStoryName(storyId)}\":\n\n");
           
            try
            {
                var chapterSave = Array.Find(savesHandler.GetSave(peerId).StoriesStats, x => x.StoryId == storyId).Chapters[chapterId];
                var chapterData = storiesHandler.GetStoryChapter(storyId, chapterId);

                stringBuilder.Append($"Полученные концовки ({chapterSave.ObtainedEndings.Length}/{chapterData.Endings.Length}):");
                for (int i = 0; i < chapterData.Endings.Length; i++)
                {
                    if (chapterSave.ObtainedEndings.Contains(i))
                    {
                        string type;
                        if (chapterData.Endings[i].Type == 0)
                            type = "ОСН";
                        else
                            type = "АЛЬТ";
                        stringBuilder.Append($"- [{type}] {chapterData.Endings[i].Name}\n");
                    }
                }

                stringBuilder.Append($"\nПолученные достижения ({chapterSave.ObtainedAchievements.Length}/{chapterData.Achievements.Length}):");
                for (int i = 0; i < chapterData.Endings.Length; i++)
                {
                    if (chapterSave.ObtainedAchievements.Contains(i))
                    {
                        stringBuilder.Append($"- {chapterData.Endings[i].Name}\n");
                    }
                }
            }
            catch (NullReferenceException)
            {
                stringBuilder.Append("- Нет данных.");
            }

            vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = stringBuilder.ToString()
            });
        }

        // Dialogs //

        /// <summary>
        /// Sends a quest choosing dialog
        /// </summary>
        /// <param name="peerId"></param>
        private void SendStoryChoiceDialog(long peerId)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Выберите историю:\n");

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(true);
            var storiesList = storiesHandler.GetAllStories();
            for (int i = 0; i < storiesList.Count; i++)
            {
                stringBuilder.Append($"[ {i + 1} ] {storiesList[i].Name}\n");
                keyboardBuilder.AddButton(
                    $"[ {i + 1} ]",
                    System.Web.HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(new SaveProgress
                    {
                        Story = i
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
            StringBuilder stringBuilder = new StringBuilder("Выберите главу:\n");
            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(true);
            foreach (var story in storiesHandler.GetAllStoryChapters(storyId))
            {
                stringBuilder.Append($"Глава {story.Chapter + 1}\n");
                keyboardBuilder.AddButton(
                    $"[ {story.Chapter + 1} ]",
                    System.Web.HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(new SaveProgress
                    {
                        Story = storyId,
                        Chapter = story.Chapter,
                        Storyline = story.Beginning,
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
                SaveProgress progress = savesHandler.GetSave(peerId).Current;

                StoryDocument story;
                if (progress.Story != null)
                {
                    if (progress.Chapter != null)
                    {
                        story = storiesHandler.GetStoryChapter((int)progress.Story, (int)progress.Chapter);

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
                        story = storiesHandler.GetStoryChapter((int)progress.Story, number);

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
        /// Handles command with arguments
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void HandleCommand(long peerId, string command, string[] args = null)
        {
            switch (command)
            {
                case "helloworld":
                    SendHelloWorld(peerId);
                    break;
                case "repeat":
                    SendContent(peerId, savesHandler.GetSave(peerId).Current);
                    break;
                case "reset":
                    SendStoryChoiceDialog(peerId);
                    break;
                case "stats":
                    switch (args.Length)
                    {
                        case 0:
                            SendStats(peerId);
                            break;
                            case 1:
                            SendStats(peerId, int.Parse(args[0]) - 1);
                            break;
                        case 2:
                            SendStats(peerId, int.Parse(args[0]) - 1, int.Parse(args[1]) - 1);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    // TODO: Send "unknown command"
                    logger.Warn("Unknown command");
                    break;
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