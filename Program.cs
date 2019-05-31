using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Targets;
using NLog.Web;
using System;
using System.IO;

namespace StoryBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText("appsettings.json")).Config;

            Logging.NLogTargetDiscord.DiscordId = config.DiscordLoggingWebhookId;
            Logging.NLogTargetDiscord.DiscordToken = config.DiscordLoggingWebhookToken;

            Target.Register<Logging.NLogTargetDiscord>("Discord");

            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

            try
            {
                logger.Debug("INIT MAIN");
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
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
    }
}
