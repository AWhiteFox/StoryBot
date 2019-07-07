using StoryBot.Extensions;
using StoryBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Keyboard;

namespace StoryBot.Logic
{
    public static class MessageBuilder
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly char Prefix = '.';

        public static (string, MessageKeyboard) BuildContent(StorylineElement storylineElement, List<string> unlockables)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string x in storylineElement.Content)
            {
                stringBuilder.Append(x + "\n");
            }
            stringBuilder.Append("\n");

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(false);
            for (int i = 0; i < storylineElement.Options.Length; i++)
            {
                var x = storylineElement.Options[i];

                if (unlockables.Contains(x.Unlocks))
                    continue;

                if (x.Needed == null)
                {
                    stringBuilder.Append($"[ {i + 1} ] {x.Content}\n");

                    keyboardBuilder.AddButton($"[ {i + 1} ]",
                        (i + 1).ToString(),
                        KeyboardButtonColor.Default);
                }
                else if (x.Needed.All(unlockables.Contains))
                {
                    stringBuilder.Append($"[ {i + 1} ] {x.Content}\n");

                    keyboardBuilder.AddButton($"[ {i + 1} ]",
                        (i + 1).ToString(),
                        KeyboardButtonColor.Positive);
                }
            }

            return (stringBuilder.ToString(), keyboardBuilder.Build());
        }

        public static (string, MessageKeyboard) BuildStorySelectDialog(List<StoryDocument> prologues)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Выберите историю из представленных:\n");

            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(false);
            for (int i = 0; i < prologues.Count; i++)
            {
                stringBuilder.Append($"[ {i + 1} ] {prologues[i].Name}\n");
                keyboardBuilder.AddButton(
                    $"[ {i + 1} ]",
                    (i + 1).ToString(),
                    KeyboardButtonColor.Primary);
            }

            return (stringBuilder.ToString(), keyboardBuilder.Build());
        }

        public static (string, MessageKeyboard) BuildEpisodeSelectDialog(List<StoryDocument> episodes, SaveStoryStats storyProgress)
        {
            StringBuilder stringBuilder = new StringBuilder();
            KeyboardBuilder keyboardBuilder = new KeyboardBuilder(false);

            stringBuilder.Append($"Выберите эпизод истории \"{episodes[0].Name}\" или используйте команду \"{Prefix}select\" для выбора другой истории:\n\n");

            // For prologue
            stringBuilder.Append($"Пролог\n");
            keyboardBuilder.AddButton(
                $"[ Пролог ]",
                "0",
                KeyboardButtonColor.Primary);

            // For other episodes
            for (int i = 1; i < episodes.Count; i++)
            {
                if (storyProgress.Episodes.Count >= i && storyProgress.Episodes[i - 1].ObtainedEndings.Contains(0))
                {
                    var episode = episodes[i];
                    stringBuilder.Append($"Эпизод {i.ToRoman()}. {episode.Name}\n");
                    keyboardBuilder.AddButton(
                        $"[ Эпизод {i.ToRoman()} ]",
                        i.ToString(),
                        KeyboardButtonColor.Primary);
                }
                else
                {
                    break;
                }
            }

            return (stringBuilder.ToString(), keyboardBuilder.Build());
        }

        public static (string, MessageKeyboard) BuildEnding(StoryDocument story, int position)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (story.Episode != 0) // Prologue check
            {
                StoryEnding ending = story.Endings[position];

                foreach (string x in ending.Content)
                {
                    stringBuilder.Append(x + "\n");
                }

                int alternativeEndingsCount = story.Endings.Length - 1;
                if (position == 0) // Check if ending canonical
                {
                    stringBuilder.Append($"\nПоздравляем, вы получили каноничную концовку \"{ending.Name}\"!\n\n");
                    stringBuilder.Append($"Этот эпизод содержит еще {alternativeEndingsCount} альтернативные концовки.");
                }
                else // Alternative
                {
                    stringBuilder.Append($"\nПоздравляем, вы получили альтернативную концовку \"{ending.Name}\"!\n\n");
                    stringBuilder.Append($"Этот эпизод содержит еще {alternativeEndingsCount - 1} альтернативные концовки и одну каноничную.");
                }
            }
            else // If it is a prologue
            {
                stringBuilder.Append("\nПоздравляем, вы завершили пролог!");
            }

            return (stringBuilder.ToString(), null /*UNDONE: Add keyboard*/);
        }

        public static string BuildAchievement(StoryAchievement achievement)
        {
            return $"Вы заработали достижение {achievement.Name}!\n - {achievement.Description}\n\n";
        }

        public static string BuildBeginningMessage()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Добро пожаловать!");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Для большего удобства, пользуйтесь клиентом поддерживающим кнопки для ботов ВКонтакте (например официальным).");
            stringBuilder.AppendLine("Возможно и управление при помощи сообщений.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Ваш прогресс будет сохраняться автоматически.");

            return stringBuilder.ToString();
        }

        public static string BuildCommandList()
        {
            StringBuilder stringBuilder = new StringBuilder("Список команд:\n\n");

            stringBuilder.AppendLine(Prefix + "select - Диалог выбора истории (Сбросит прогресс текущего эпизода!)");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(Prefix + "repeat - Заново отправляет сообщений с диалогом выбора для текущей истории");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(Prefix + "list - Список всех историй и ваша статистика по ним");
            stringBuilder.AppendLine(Prefix + "list <номер_истории> - Список всех эпизодов истории и ваша статистика по ним");
            stringBuilder.AppendLine(Prefix + "list <номер_истории> <номер_эпизода (0 для пролога)> - Ваша статистика по эпизоду");

            return stringBuilder.ToString();
        }

        public static string BuildStats(List<StoryDocument> prologues, List<SaveStoryStats> storiesStats)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Общая статистика:\n\n");

            foreach (var prologue in prologues)
            {
                int completedEpisodes = 0;
                var episodes = storiesStats.Find(x => x.StoryId == prologue.StoryId).Episodes;
                foreach (var episode in episodes)
                {
                    if (episode.ObtainedEndings.Contains(0))
                        completedEpisodes++;
                }

                stringBuilder.Append($"{prologue.StoryId}. {prologue.Name} - Пройдено эпизодов: {completedEpisodes}\n");
            }

            return stringBuilder.ToString();
        }

        public static string BuildStoryStats(List<StoryDocument> episodes, List<SaveEpisodeStats> episodesStats)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"Статистика по \"{episodes[0].Name}\":\n");

            try
            {
                // Prologue
                if (episodesStats[0].ObtainedEndings.Contains(0))
                {
                    stringBuilder.Append($"- Пролог. Завершено\n");
                }
                else
                {
                    stringBuilder.Append($"- Пролог. Не завершено\n");
                }

                int i = 1;
                // Completed
                for (; i < episodes.Count; i++)
                {
                    if (!episodesStats[i].ObtainedEndings.Contains(0))
                    {
                        break;
                    }
                    var episode = episodes[i];
                    stringBuilder.Append($"- Эпизод {i.ToRoman()}. {episode.Name}: {episodesStats[i].ObtainedEndings.Count}/{episode.Endings.Length} концовок, {episodesStats[i].ObtainedAchievements.Count}/{episode.Achievements.Length} достижений\n");
                }
                // Not completed
                for (; i < episodes.Count; i++)
                {
                    var episode = episodes[i];
                    stringBuilder.Append($"- Эпизод {i.ToRoman()} [НЕ ОТКРЫТО]");
                }
            }
            catch (NullReferenceException)
            {
                stringBuilder.Append("\n- Нет данных.");
                logger.Warn("No data for stats");
            }

            return stringBuilder.ToString();
        }

        public static string BuildEpisodeStats(StoryDocument episodeData, SaveEpisodeStats episodeStats)
        {
            if (episodeData.Episode == 0)
            {
                return "Нельзя получить статистику по прологу";
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"Статистика по эпизоду {episodeData.Episode.ToRoman()}. {episodeData.Name}:\n\n");

            try
            {
                stringBuilder.Append($"Полученные концовки ({episodeStats.ObtainedEndings.Count}/{episodeData.Endings.Length}):\n");
                for (int i = 0; i < episodeData.Endings.Length; i++)
                {
                    if (episodeStats.ObtainedEndings.Contains(i))
                    {
                        string type;
                        if (i == 0)
                            type = "ОСН";
                        else
                            type = "АЛЬТ";
                        stringBuilder.Append($"- [{type}] \"{episodeData.Endings[i].Name}\"\n");
                    }
                }

                stringBuilder.Append($"\nПолученные достижения ({episodeStats.ObtainedAchievements.Count}/{episodeData.Achievements.Length}):\n");
                for (int i = 0; i < episodeData.Endings.Length; i++)
                {
                    if (episodeStats.ObtainedAchievements.Contains(i))
                    {
                        stringBuilder.Append($"- \"{episodeData.Endings[i].Name}\"\n");
                    }
                }
            }
            catch (NullReferenceException)
            {
                stringBuilder.Append("- Нет данных.");
            }

            return stringBuilder.ToString();
        }
    }
}
