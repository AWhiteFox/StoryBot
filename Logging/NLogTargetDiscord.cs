using NLog;
using NLog.Config;
using NLog.Targets;

namespace StoryBot.Logging
{
    [Target("Discord")]
    public sealed class NLogTargetDiscord : TargetWithLayout
    {
        private DiscordWebhook discord;

        public NLogTargetDiscord()
        {
            if (DiscordId != null && DiscordToken != null)
            {
                discord = new DiscordWebhook(DiscordId, DiscordToken);
            }
        }

        public static string DiscordId { get; set; }

        public static string DiscordToken { get; set; }

        protected override void Write(LogEventInfo logEvent)
        {
            string logMessage = this.Layout.Render(logEvent);
            discord.Send(logMessage);
        }
    }
}
