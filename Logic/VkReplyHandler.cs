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
        /// <summary>
        /// VK API
        /// </summary>
        private readonly IVkApi vk;

        /// <summary>
        /// Gets stories
        /// </summary>
        private readonly IStoriesHandler stories;

        /// <summary>
        /// Gets and updates saves
        /// </summary>
        private readonly ISavesHandler saves;

        /// <summary>
        /// Message generator
        /// </summary>
        private readonly VkMessageBuilder messageBuilder;

        /// <summary>
        /// Command prefix
        /// </summary>
        public readonly char Prefix = Environment.GetEnvironmentVariable("BOT_PREFIX")[0];

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="vk"></param>
        /// <param name="stories"></param>
        /// <param name="saves"></param>
        public VkReplyHandler(IVkApi vk, IStoriesHandler stories, ISavesHandler saves)
        {
            this.vk = vk;
            this.stories = stories;
            this.saves = saves;
            this.messageBuilder = new VkMessageBuilder(Prefix);
        }

        // Replies //

        /// <summary>
        /// Sends response to provided number
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="number"></param>
        public void ReplyToNumber(long peerId, int number)
        {
            // Try-catch block: if something goes wrong replies with "Index not found"
            try
            {
                // Getting save
                var save = saves.Get(peerId);

                if (save.Current.Story != null) // IF story already selected
                {
                    if (save.Current.Episode != null) // IF episode already selected
                    {
                        // Getting story from DB
                        StoryDocument story = stories.GetEpisode(save.Current.Story.Value, save.Current.Episode.Value);

                        // Finding selected option
                        StoryOption selectedOption = story.GetStoryline(save.Current.Storyline)
                                .Elements[save.Current.Position]
                                .Options[number - 1];

                        if (selectedOption.Storyline == "Ending") // If selected option is ending...
                        {
                            // Add ending to progress
                            save.AddEnding(story.StoryId, story.Episode, selectedOption.Position.Value);

                            // Send messages
                            Send(peerId, messageBuilder.BuildEnding(story, selectedOption.Position.Value));
                            var episodes = stories.GetStoryEpisodes(story.StoryId);
                            var progress = save.GetStoryStats(story.StoryId);
                            Send(peerId, messageBuilder.BuildEpisodeSelectDialog(episodes, progress));
                        }
                        else // Default case
                        {
                            // Updating save progress
                            if (selectedOption.Storyline != null)
                                save.Current.Storyline = selectedOption.Storyline;
                            if (selectedOption.Position != null)
                                save.Current.Position = selectedOption.Position.Value;

                            // "Needed" check
                            if (selectedOption.Needed != null && !selectedOption.Needed.All(save.Current.Unlockables.Contains))
                                goto IndexNotFound;

                            // "Unlocks" handling
                            if (!string.IsNullOrEmpty(selectedOption.Unlocks))
                            {
                                if (!save.Current.Unlockables.Contains(selectedOption.Unlocks))
                                    save.AddUnlockable(selectedOption.Unlocks);
                                else
                                    goto IndexNotFound;
                            }

                            var storylineElement = story.GetStoryline(save.Current.Storyline).Elements[save.Current.Position];
                            (string content, MessageKeyboard keyboard) message = messageBuilder.BuildContent(storylineElement, save.Current.Unlockables);

                            // If selected option contains an achievement
                            if (selectedOption.Achievement != null)
                            {
                                // Add achievement to save stats
                                save.AddAchievement(story.StoryId, story.Episode, selectedOption.Achievement.Value);
                                // Edit response message
                                message.content = messageBuilder.BuildAchievement(story.Achievements[selectedOption.Achievement.Value]) + message.content;
                            }

                            // Send created message
                            Send(peerId, message);
                        }
                    }
                    else // From episode selection
                    {
                        // Check that previous episode's canonical ending completed
                        if (number == 0 || save.GetStoryStats(save.Current.Story.Value).Episodes[number - 1].ObtainedEndings.Contains(0))
                        {
                            // Get episode by provided number
                            StoryDocument story = stories.GetEpisode(save.Current.Story.Value, number);

                            // Update current progress
                            save.Current.Episode = number;
                            save.Current.Storyline = story.Storylines[0].Tag;
                            save.Current.Position = 0;

                            // Send message
                            Send(peerId, messageBuilder.BuildContent(story.Storylines[0].Elements[0], save.Current.Unlockables));
                        }
                        else
                            goto IndexNotFound;
                    }
                }
                else // From story selection
                {
                    // Find story progress
                    var stats = save.GetStoryStats(number);

                    // Get all story episodes
                    var episodes = stories.GetStoryEpisodes(number);
                    save.Current.Story = number;
                    
                    // Send episode select dialog
                    Send(peerId, messageBuilder.BuildEpisodeSelectDialog(episodes, stats));
                }

                // Finally update save
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

        IndexNotFound: // If something went wrong...
            Send(peerId, "Выберите вариант из представленных");
        }

        /// <summary>
        /// Sends response to provided command
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="command"></param>
        public void ReplyToCommand(long peerId, string command)
        {
            // Try-catch: If something goes wrong send "Wrong command usage" message
            try
            {
                // Split command to alias and arguments
                string[] splittedCommand = command.Split(" ");
                string alias = splittedCommand[0];
                string[] args = splittedCommand.Skip(1).ToArray();

                switch (alias)
                {
                    case "list":
                        // Arguments length check...
                        if (args.Length == 0)
                        {
                            // Send stories stats
                            Send(peerId,
                                    messageBuilder.BuildStats(stories.GetAllPrologues(),
                                    saves.Get(peerId).StoriesStats));
                        }
                        else if (args.Length == 1)
                        {
                            // Parse story ID from first argument
                            var storyId = int.Parse(args[0]);
                            // Send story episodes stats
                            Send(peerId,
                                messageBuilder.BuildStoryStats(stories.GetStoryEpisodes(storyId),
                                saves.Get(peerId).GetStoryStats(storyId).Episodes));
                        }
                        else if (args.Length == 2)
                        {
                            // Parse story ID from first argument
                            var storyId = int.Parse(args[0]);
                            // Parse episode ID from second argument
                            var episodeId = int.Parse(args[1]);
                            // Send episode stats
                            Send(peerId,
                                messageBuilder.BuildEpisodeStats(stories.GetEpisode(storyId, episodeId),
                                saves.Get(peerId).GetStoryStats(storyId).Episodes[episodeId]));
                        }
                        else
                        {
                            // Send command list
                            Send(peerId, messageBuilder.BuildCommandList());
                        }
                        break;
                    case "repeat":
                        {
                            // Get save
                            var save = saves.Get(peerId);
                            if (save.Current.Story != null)
                            {
                                if (save.Current.Episode != null) // Default case
                                {
                                    var storylineElement = stories.GetEpisode(save.Current.Story.Value, save.Current.Episode.Value)
                                        .GetStoryline(save.Current.Storyline).Elements[save.Current.Position];
                                    Send(peerId,
                                        messageBuilder.BuildContent(storylineElement, save.Current.Unlockables));
                                }
                                else // Episode select
                                {
                                    var episodes = stories.GetStoryEpisodes(save.Current.Story.Value);
                                    var stats = save.GetStoryStats(save.Current.Story.Value);

                                    Send(peerId,
                                        messageBuilder.BuildEpisodeSelectDialog(episodes, stats));
                                }
                            }
                            else // Story select
                            {
                                Send(peerId, messageBuilder.BuildStorySelectDialog(stories.GetAllPrologues()));
                            }
                        }
                        return;
                    case "select":
                        {
                            // Reset progress
                            var save = saves.Get(peerId);
                            save.Current = new SaveProgress();
                            saves.Update(save);

                            // Send story select dialog
                            Send(peerId, messageBuilder.BuildStorySelectDialog(stories.GetAllPrologues()));
                            return;
                        }
                    default:
                        {
                            // Send command list if command not found
                            Send(peerId, messageBuilder.BuildCommandList());
                            return;
                        } 
                }
            }
            catch (Exception)
            {
                Send(peerId, "Неправильное использование команды.");
            }
        }

        /// <summary>
        /// Used when user starts conversation with bot
        /// </summary>
        /// <param name="peerId"></param>
        public void ReplyFirstMessage(long peerId)
        {
            // Create new save
            saves.CreateNew(peerId);

            // Send messages
            Send(peerId, messageBuilder.BuildBeginningMessage());
            Send(peerId, messageBuilder.BuildCommandList());
            Send(peerId, messageBuilder.BuildStorySelectDialog(stories.GetAllPrologues()));
        }

        /// <summary>
        /// Sends message with error
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="exception"></param>
        public void ReplyWithError(long peerId, Exception exception)
        {
            Send(peerId, $"Во время обработки вашего сообщения произошла непредвиденная ошибка:\n\n{exception.Message}\n\nПожалуйста сообщите администрации");
        }

        // Utils //

        /// <summary>
        /// Returns true if provided message is last in conversation
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool CheckThatMessageIsLast(Message msg)
        {
            // Compare message dates...
            return vk.Messages.GetHistory(new MessagesGetHistoryParams { Count = 1, PeerId = msg.PeerId }).Messages.Single().Date <= msg.Date;
        }

        // Private Methods //

        /// <summary>
        /// Sends message with content
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="message"></param>
        private void Send(long peerId, string message)
        {
            vk.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = peerId,
                Message = message
            });
        }

        /// <summary>
        /// Sends message with content and keyboard
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="message"></param>
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