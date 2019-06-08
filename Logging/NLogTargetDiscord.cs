using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Text;

namespace StoryBot.Logging
{
    [Target("Discord")]
    public sealed class NLogTargetDiscord : TargetWithLayout
    {
        private readonly DiscordWebhook discord;

        public NLogTargetDiscord()
        {
            discord = new DiscordWebhook(Environment.GetEnvironmentVariable("DISCORD_WEBHOOKID"),
                                         Environment.GetEnvironmentVariable("DISCORD_WEBHOOKTOKEN"));
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent.Exception != null)
            {
                string[] splitted = logEvent.Exception.ToString().Split(System.Environment.NewLine);

                StringBuilder stringBuilder = new StringBuilder();
                List<string> sendingList = new List<string>();
                foreach (string x in splitted)
                {
                    if (stringBuilder.Length + x.Length < 2000)
                    {
                        stringBuilder.Append(x + "\n");
                    }
                    else
                    {
                        sendingList.Add(stringBuilder.ToString());
                        stringBuilder.Clear();
                    }
                }
                sendingList.Add(stringBuilder.ToString());

                logEvent.Exception = null;
                discord.Send(Layout.Render(logEvent));

                foreach (string x in sendingList)
                {
                    discord.Send($"```csharp\n{x}\n```");
                }
            }
            else
            {
                discord.Send(Layout.Render(logEvent));
            }
        }
    }
}
