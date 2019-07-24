using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StoryBot.Core.Logic;
using StoryBot.Core.Model;
using StoryBot.Vk.Logic;
using System;
using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace StoryBot.Vk
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSingleton(sp =>
            {
                var vkApi = new VkApi();
                vkApi.Authorize(new ApiAuthParams { AccessToken = Environment.GetEnvironmentVariable("VK_ACCESSTOKEN") });
            
                var database = new MongoClient(Environment.GetEnvironmentVariable("MONGODB_URI")).GetDatabase("StoryBot");
                var prefix = Environment.GetEnvironmentVariable("BOT_PREFIX")[0];

                var replyHandler = new ReplyHandler<MessagesSendParams>(
                    new StoriesContext(database.GetCollection<StoryDocument>("stories")),
                    new SavesContext(database.GetCollection<SaveDocument>("vkSaves")),
                    new VkMessageBuilder(prefix),
                    new VkMessageSender(vkApi),
                    prefix);

                return new VkEventsHandler(vkApi, replyHandler);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
