using Microsoft.AspNetCore.Mvc;
using StoryBot.Logic;
using StoryBot.Model;
using System;
using VkNet.Abstractions;

namespace StoryBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly MessagesHandler messagesHandler;

        public CallbackController(IVkApi vkApi, MongoDB.Driver.IMongoDatabase database)
        {
            messagesHandler = new MessagesHandler(vkApi,
                new StoriesHandler(database.GetCollection<StoryDocument>("stories")),
                new SavesHandler(database.GetCollection<SaveDocument>("saves")));
        }

        [HttpPost]
        public IActionResult Post([FromBody] CallbackUpdate update)
        {
            try
            {
                switch (update.Type)
                {
                    case "confirmation":
                        return Ok(Environment.GetEnvironmentVariable("VK_CONFIGURATION"));
                    case "message_new":
                        messagesHandler.HandleNew(update.Object);
                        return Ok("ok");
                    default:
                        return BadRequest("Unknown event");
                }
            }
            catch (Exception exception)
            {
                string str = "An error occurred while handling request";
                logger.Error(exception, str);
                return BadRequest(str);
            }            
        }
    }
}