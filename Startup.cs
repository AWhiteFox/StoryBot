using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StoryBot.Logic;
using StoryBot.Model;
using System;
using VkNet;
using VkNet.Model;

namespace StoryBot
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

            services.AddDataProtection().PersistKeysToFileSystem(new System.IO.DirectoryInfo(@"\keys\"));

            services.AddSingleton(sp =>
            {
                VkApi api = new VkApi();
                api.Authorize(new ApiAuthParams { AccessToken = Environment.GetEnvironmentVariable("VK_ACCESSTOKEN") });

                var database = new MongoClient(Environment.GetEnvironmentVariable("MONGODB_URI")).GetDatabase("StoryBot");
                return new EventsHandler(new ReplyHandler(
                    api,
                    new StoriesHandler(database.GetCollection<StoryDocument>("stories")),
                    new SavesHandler(database.GetCollection<SaveDocument>("saves"))));
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
