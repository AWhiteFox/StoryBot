using StoryBot.Abstractions;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace StoryBot.Logic
{
    public class ReplyHandler
    {
        private readonly IVkApi vk;

        private readonly IStoriesHandler stories;

        private readonly ISavesHandler saves;

        public ReplyHandler(IVkApi vk, IStoriesHandler stories, ISavesHandler saves)
        {
            this.vk = vk;
            this.stories = stories;
            this.saves = saves;
        }

        // Replies //

        public void ReplyToNumber(long peerId, int number)
        {
            try
            {
                var save = saves.Get(peerId);

                StoryDocument story;
                if (save.Current.Story != null)
                {
                    if (save.Current.Episode != null)
                    {
                        story = stories.GetEpisode(save.Current.Story.Value, save.Current.Episode.Value);

                        StoryOption selectedOption = Array
                                .Find(story.Storylines, x => x.Tag == (save.Current.Storyline ?? story.Storylines[0].Tag))
                                .Elements[save.Current.Position]
                                .Options[number - 1];

                        if (selectedOption.Storyline == "Ending") // Ending
                        {
                            save.AddEnding(story.StoryId, story.Episode, selectedOption.Position.Value);
                            Send(peerId, MessageBuilder.BuildEnding(story, selectedOption.Position.Value));
                            Send(peerId,
                                    MessageBuilder.BuildEpisodeSelectDialog(stories.GetStoryEpisodes(story.StoryId),
                                    save.StoriesStats.Find(x => x.StoryId == story.StoryId)));
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

                            (string, MessageKeyboard) message = MessageBuilder.BuildContent(Array.Find(story.Storylines, x => x.Tag == save.Current.Storyline)
                                .Elements[save.Current.Position], save.Current.Unlockables);

                            if (selectedOption.Achievement != null)
                            {
                                save.AddAchievement(story.StoryId, story.Episode, selectedOption.Achievement.Value);
                                message.Item1 = MessageBuilder.BuildAchievement(story.Achievements[selectedOption.Achievement.Value]) + message.Item1;
                            }

                            Send(peerId, message);
                        }
                    }
                    else // Episode choice
                    {
                        // Check that previous episode's canonical ending completed
                        if (number == 0 || save.StoriesStats.Find(x => x.StoryId == save.Current.Story).Episodes[number - 1].ObtainedEndings.Contains(0))
                        {
                            story = stories.GetEpisode(save.Current.Story.Value, number);

                            Send(peerId,
                                MessageBuilder.BuildContent(story.Storylines[0].Elements[0], save.Current.Unlockables));

                            save.Current.Episode = number;
                            save.Current.Storyline = story.Storylines[0].Tag;
                            save.Current.Position = 0;
                        }
                        else
                            goto IndexNotFound;
                    }
                }
                else // Story choice
                {
                    var episodes = stories.GetStoryEpisodes(number);

                    var stats = save.StoriesStats.Find(x => x.StoryId == number);
                    if (stats == null)
                    {
                        stats = new SaveStoryStats(number);
                        save.StoriesStats.Add(stats);
                    }

                    Send(peerId, MessageBuilder.BuildEpisodeSelectDialog(episodes, stats));
                    save.Current.Story = number;
                }

                save.Update();
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
                    case "list":
                        int storyId, episodeId;
                        switch (args.Length)
                        {
                            case 0:
                                Send(peerId,
                                    MessageBuilder.BuildStats(stories.GetAllPrologues(),
                                    saves.Get(peerId).StoriesStats));
                                return;
                            case 1:
                                storyId = int.Parse(args[0]);
                                Send(peerId,
                                    MessageBuilder.BuildStoryStats(stories.GetStoryEpisodes(storyId),
                                    saves.Get(peerId).StoriesStats.Find(x => x.StoryId == storyId).Episodes));
                                return;
                            case 2:
                                storyId = int.Parse(args[0]);
                                episodeId = int.Parse(args[1]);
                                Send(peerId,
                                    MessageBuilder.BuildEpisodeStats(stories.GetEpisode(storyId, episodeId),
                                    saves.Get(peerId).StoriesStats.Find(x => x.StoryId == storyId).Episodes[episodeId]));
                                return;
                            default:
                                Send(peerId, MessageBuilder.BuildCommandList());
                                return;
                        }
                    case "repeat":
                        var progress = saves.Get(peerId).Current;
                        if (progress.Story != null)
                        {
                            if (progress.Episode != null)
                            {
                                Send(peerId, MessageBuilder.BuildContent(Array.Find(stories.GetEpisode(progress.Story.Value, progress.Episode.Value).Storylines, x => x.Tag == progress.Storyline)
                                    .Elements[progress.Position], progress.Unlockables));
                            }
                            else
                            {
                                Send(peerId,
                                    MessageBuilder.BuildEpisodeSelectDialog(stories.GetStoryEpisodes(progress.Story.Value),
                                    saves.Get(peerId).StoriesStats.Find(x => x.StoryId == progress.Story)));
                            }
                        }
                        else
                        {
                            Send(peerId, MessageBuilder.BuildStorySelectDialog(stories.GetAllPrologues()));
                        }
                        return;
                    case "select":
                        var save = saves.Get(peerId);
                        save.Current = new SaveProgress();
                        save.Update();

                        Send(peerId, MessageBuilder.BuildStorySelectDialog(stories.GetAllPrologues()));
                        return;
                    case "helloworld":
                        Send(peerId, "Hello, World!");
                        return;
                    default:
                        Send(peerId, MessageBuilder.BuildCommandList());
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
            Send(peerId, MessageBuilder.BuildBeginningMessage());
            Send(peerId, MessageBuilder.BuildCommandList());
            Send(peerId, MessageBuilder.BuildStorySelectDialog(stories.GetAllPrologues()));
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

        private void Send(long peerId, (string, MessageKeyboard) message)
        {
            vk.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = message.Item1,
                Keyboard = message.Item2
            });
        }
    }
}