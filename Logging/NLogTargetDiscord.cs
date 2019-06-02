using NLog;
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
            if (logEvent.Exception != null)
            {
                logEvent.Message += System.Environment.NewLine +
                    "```csharp" + System.Environment.NewLine + 
                    logEvent.Exception.ToString() + System.Environment.NewLine + 
                    "```";
            }
            string logMessage = Layout.Render(logEvent);
            discord.Send(logMessage);
        }
    }
}
