using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StoryBot.Core.Logic;
using StoryBot.Core.Model;
using StoryBot.Vk.Logic;
using StoryBot.Vk.Vk.Logic;
using System;
using VkNet;
using VkNet.Model;

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
                VkApi api = new VkApi();
                api.Authorize(new ApiAuthParams { AccessToken = Environment.GetEnvironmentVariable("VK_ACCESSTOKEN") });

                var database = new MongoClient(Environment.GetEnvironmentVariable("MONGODB_URI")).GetDatabase("StoryBot");
                return new VkEventsHandler(new VkReplyHandler(
                    api,
                    new StoriesHandler(database.GetCollection<StoryDocument>("stories")),
                    new SavesHandler(database.GetCollection<SaveDocument>("vkSaves"))));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Disabled because I don't have SSL certificate 
            // app.UseHttpsRedirection();

            app.UseMvc();
        }
    }
}
