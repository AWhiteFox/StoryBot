using StoryBot.Core.Abstractions;
using StoryBot.Core.Model;
using StoryBot.Vk.Logic;
using System;
using System.Linq;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;


namespace StoryBot.Vk.Vk.Logic
{
    public class VkReplyHandler
    {
        private readonly IVkApi vk;

        private readonly IStoriesHandler stories;

        private readonly ISavesHandler saves;

        private readonly VkMessageBuilder messageBuilder;

        public readonly char Prefix = Environment.GetEnvironmentVariable("BOT_PREFIX")[0];

        public VkReplyHandler(IVkApi vk, IStoriesHandler stories, ISavesHandler saves)
        {
            this.vk = vk;
            this.stories = stories;
            this.saves = saves;
            this.messageBuilder = new VkMessageBuilder(Prefix); // TEMP: Temporary MessageBuilder creation
        }

        // Replies //

        public void ReplyToNumber(long peerId, int number)
        {
            try
            {
                // Getting save from DB
                var save = saves.Get(peerId);

                if (save.Current.Story != null)
                {
                    if (save.Current.Episode != null)
                    {
                        // Getting story from DB
                        StoryDocument story = stories.GetEpisode(save.Current.Story.Value, save.Current.Episode.Value);

                        StoryOption selectedOption = story.GetStoryline(save.Current.Storyline)
                                .Elements[save.Current.Position]
                                .Options[number - 1];

                        if (selectedOption.Storyline == "Ending") // Ending
                        {
                            save.AddEnding(story.StoryId, story.Episode, selectedOption.Position.Value);

                            Send(peerId, messageBuilder.BuildEnding(story, selectedOption.Position.Value));

                            var episodes = stories.GetStoryEpisodes(story.StoryId);
                            var progress = save.StoriesStats.Find(x => x.StoryId == story.StoryId);
                            Send(peerId, messageBuilder.BuildEpisodeSelectDialog(episodes, progress));
                        }
                        else // Default case
                        {
                            if (selectedOption.Storyline != null)
                                save.Current.Storyline = selectedOption.Storyline;

                            if (selectedOption.Position != null)
                                save.Current.Position = selectedOption.Position.Value;

                            // "Needed" check
                            if (selectedOption.Needed != null && !selectedOption.Needed.All(save.Current.Unlockables.Contains))
                                goto IndexNotFound;

                            // "Unlocks" handling
                            if (!string.IsNullOrEmpty(selectedOption.Unlocks) && !save.Current.Unlockables.Contains(selectedOption.Unlocks))
                                save.AddUnlockable(selectedOption.Unlocks);

                            var storylineElement = story.GetStoryline(save.Current.Storyline).Elements[save.Current.Position];
                            (string content, MessageKeyboard keyboard) message = messageBuilder.BuildContent(storylineElement, save.Current.Unlockables);

                            if (selectedOption.Achievement != null)
                            {
                                save.AddAchievement(story.StoryId, story.Episode, selectedOption.Achievement.Value);
                                message.content = messageBuilder.BuildAchievement(story.Achievements[selectedOption.Achievement.Value]) + message.content;
                            }

                            Send(peerId, message);
                        }
                    }
                    else // From episode selection
                    {
                        // Check that previous episode's canonical ending completed
                        if (number == 0 || save.StoriesStats.Find(x => x.StoryId == save.Current.Story).Episodes[number - 1].ObtainedEndings.Contains(0))
                        {
                            StoryDocument story = stories.GetEpisode(save.Current.Story.Value, number);

                            Send(peerId, messageBuilder.BuildContent(story.Storylines[0].Elements[0], save.Current.Unlockables));

                            save.Current.Episode = number;
                            save.Current.Storyline = story.Storylines[0].Tag;
                            save.Current.Position = 0;
                        }
                        else
                            goto IndexNotFound;
                    }
                }
                else // From story slection
                {
                    var progress = save.StoriesStats.Find(x => x.StoryId == number);
                    if (progress == null)
                    {
                        progress = new SaveStoryStats(number);
                        save.StoriesStats.Add(progress);
                    }

                    var episodes = stories.GetStoryEpisodes(number);
                    Send(peerId, messageBuilder.BuildEpisodeSelectDialog(episodes, progress));
                    save.Current.Story = number;
                }

                saves.Update(save);
                return;
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException)
                {
                    goto IndexNotFound;
                }
                else throw;
            }

        IndexNotFound:
            Send(peerId, "Выберите вариант из представленных");
        }

        public void ReplyToCommand(long peerId, string command)
        {
            try
            {
                string[] splittedCommand = command.Split(" ");
                string alias = splittedCommand[0];
                string[] args = splittedCommand.Skip(1).ToArray();

                switch (alias)
                {
                    case "list":;
                        if (args.Length == 0)
                        {
                            Send(peerId,
                                    messageBuilder.BuildStats(stories.GetAllPrologues(),
                                        saves.Get(peerId).StoriesStats));
                        }
                        else if (args.Length == 1)
                        {
                            var storyId = int.Parse(args[0]);
                            Send(peerId,
                                messageBuilder.BuildStoryStats(stories.GetStoryEpisodes(storyId),
                                    saves.Get(peerId).StoriesStats.Find(x => x.StoryId == storyId).Episodes));
                        }
                        else if (args.Length == 2)
                        {
                            var storyId = int.Parse(args[0]);
                            var episodeId = int.Parse(args[1]);
                            Send(peerId,
                                messageBuilder.BuildEpisodeStats(stories.GetEpisode(storyId, episodeId),
                                    saves.Get(peerId).StoriesStats.Find(x => x.StoryId == storyId).Episodes[episodeId]));
                        }
                        else
                        {
                            Send(peerId, messageBuilder.BuildCommandList());
                        }
                        break;
                    case "repeat":
                        {
                            var save = saves.Get(peerId);
                            if (save.Current.Story != null)
                            {
                                if (save.Current.Episode != null)
                                {
                                    var storylineElement = stories.GetEpisode(save.Current.Story.Value, save.Current.Episode.Value)
                                        .GetStoryline(save.Current.Storyline).Elements[save.Current.Position];
                                    Send(peerId,
                                        messageBuilder.BuildContent(storylineElement, save.Current.Unlockables));
                                }
                                else
                                {
                                    var episodes = stories.GetStoryEpisodes(save.Current.Story.Value);
                                    var stats = save.StoriesStats.Find(x => x.StoryId == save.Current.Story);

                                    Send(peerId,
                                        messageBuilder.BuildEpisodeSelectDialog(episodes, stats));
                                }
                            }
                            else
                            {
                                Send(peerId, messageBuilder.BuildStorySelectDialog(stories.GetAllPrologues()));
                            }
                        }
                        return;
                    case "select":
                        {
                            var save = saves.Get(peerId);
                            save.Current = new SaveProgress();
                            saves.Update(save);

                            Send(peerId, messageBuilder.BuildStorySelectDialog(stories.GetAllPrologues()));
                            return;
                        }
                    default:
                        Send(peerId, messageBuilder.BuildCommandList());
                        return;
                }
            }
            catch (Exception)
            {
                Send(peerId, "Неправильное использование команды.");
            }
        }

        public void ReplyFirstMessage(long peerId)
        {
            saves.CreateNew(new SaveDocument(peerId));
            var prologues = stories.GetAllPrologues();

            Send(peerId, messageBuilder.BuildBeginningMessage());
            Send(peerId, messageBuilder.BuildCommandList());
            Send(peerId, messageBuilder.BuildStorySelectDialog(prologues));
        }

        public void ReplyWithError(long peerId, Exception exception)
        {
            Send(peerId, $"Во время обработки вашего сообщения произошла непредвиденная ошибка:\n\n{exception.Message}\n\nПожалуйста сообщите администрации");
        }

        // Utils //

        public bool CheckThatMessageIsLast(Message msg)
        {
            return vk.Messages.GetHistory(new MessagesGetHistoryParams { Count = 1, PeerId = msg.PeerId }).Messages.Single().Date <= msg.Date;
        }

        // Private Methods //

        private void Send(long peerId, string message)
        {
            vk.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = message
            });
        }

        private void Send(long peerId, (string content, MessageKeyboard keyboard) message)
        {
            vk.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = message.content,
                Keyboard = message.keyboard
            });
        }
    }
}