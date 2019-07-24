using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;

namespace StoryBot.Vk
{
    public class Program
    {
        public static void Main(string[] args)
        {
            NLog.Logger logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            ConfigureNLogDiscord();

            try
            {
                logger.Info("Initialization...");
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .UseNLog();

        private static void ConfigureNLogDiscord()
        {
            NLog.LogManager.Configuration.AddTarget("DiscordNotCritical", new NLog.Discord.WebhookTarget
            {
                UseEmbeds = false,
                Layout = "${time} | **${uppercase:${level}}** | ${logger} | ${message}",
                Id = ulong.Parse(Environment.GetEnvironmentVariable("DISCORD_WEBHOOKID")),
                Token = Environment.GetEnvironmentVariable("DISCORD_WEBHOOKTOKEN")
            });
            NLog.LogManager.Configuration.AddTarget("DiscordCritical", new NLog.Discord.WebhookTarget
            {
                Id = ulong.Parse(Environment.GetEnvironmentVariable("DISCORD_WEBHOOKID")),
                Token = Environment.GetEnvironmentVariable("DISCORD_WEBHOOKTOKEN")
            });
            NLog.LogManager.Configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Info, "DiscordNotCritical");
            NLog.LogManager.Configuration.AddRule(NLog.LogLevel.Warn, NLog.LogLevel.Fatal, "DiscordCritical");
            NLog.LogManager.ReconfigExistingLoggers();
        }
    }
}
