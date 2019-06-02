using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using StoryBot.Callback;
using StoryBot.Messaging;
using System;
using VkNet.Abstractions;

namespace StoryBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IConfiguration configuration;

        private readonly CallbackHandler callbackHandler;

        public MessageHandler messagesHandler;

        public CallbackController(IConfiguration configuration, IVkApi vkApi, IMongoDatabase database)
        {
            this.configuration = configuration;
            callbackHandler = new CallbackHandler(vkApi, database);
        }

        [HttpPost]
        public IActionResult Post([FromBody] Update update)
        {
            try
            {
                switch (update.Type)
                {
                    case "confirmation":
                        return Ok(configuration["Config:Confirmation"]);
                    case "message_new":
                        callbackHandler.NewMessage(update.Object);
                        break;
                }
                return Ok("ok");
            }
            catch (Exception exception)
            {
                logger.Error(exception, "An error occured while handling request");
                return BadRequest();
            }            
        }
    }
}