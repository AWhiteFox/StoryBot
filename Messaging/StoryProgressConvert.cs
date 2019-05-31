using System;
using System.Text;
using static StoryBot.Messaging.DatabaseObjects;

namespace StoryBot.Messaging
{
    public static class StoryProgressConvert
    {
        public static StoryProgress Deserialize(string str)
        {
            string[] splitted = { null, null, null };
            string[] _splitted = str.Split('.');

            Array.Copy(_splitted, splitted, _splitted.Length);

            StoryProgress progress = new StoryProgress
            {
                story = splitted[0],
                storyline = splitted[1],
                position = !string.IsNullOrEmpty(splitted[2]) ? int.Parse(splitted[2]) : 0
            };
            return progress;
        }

        public static string Serialize(StoryProgress progress)
        {
            StringBuilder sb;
            if (string.IsNullOrEmpty(progress.story))
            {
                return string.Empty;
            }
            else
            {
                sb = new StringBuilder();
                sb.Append(progress.story);
            }
            if (!string.IsNullOrEmpty(progress.storyline))
            {
                sb.Append('.');
                sb.Append(progress.storyline);
                sb.Append('.');
                sb.Append(progress.position.ToString());
            }
            return sb.ToString();
        }
    }
}
