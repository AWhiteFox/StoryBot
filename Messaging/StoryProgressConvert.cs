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
                Story = splitted[0],
                Storyline = splitted[1],
                Position = !string.IsNullOrEmpty(splitted[2]) ? int.Parse(splitted[2]) : 0
            };
            return progress;
        }

        public static string Serialize(StoryProgress progress)
        {
            StringBuilder sb;
            if (string.IsNullOrEmpty(progress.Story))
            {
                return string.Empty;
            }
            else
            {
                sb = new StringBuilder();
                sb.Append(progress.Story);
            }
            if (!string.IsNullOrEmpty(progress.Storyline))
            {
                sb.Append('.');
                sb.Append(progress.Storyline);
                sb.Append('.');
                sb.Append(progress.Position.ToString());
            }
            return sb.ToString();
        }
    }
}
