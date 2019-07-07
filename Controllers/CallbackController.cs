using Microsoft.AspNetCore.Mvc;
using StoryBot.Logic;
using StoryBot.Model;
using System;

namespace StoryBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string secret = Environment.GetEnvironmentVariable("VK_SECRET");

        private readonly EventsHandler eventsHandler;

        public CallbackController(EventsHandler eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }

        [HttpPost]
        public IActionResult Post([FromBody] CallbackUpdate update)
        {
            if (update.Secret == secret)
            {
                try
                {
                    switch (update.Type)
                    {
                        case "confirmation":
                            return Ok(Environment.GetEnvironmentVariable("VK_CONFIRMATION"));
                        case "message_new":
                            eventsHandler.MessageNewEvent(update.Object);
                            return Ok("ok");
                        case "message_allow":
                            eventsHandler.MessageAllowEvent(update.Object);
                            return Ok("ok");
                        default:
                            logger.Warn($"Unknown event type '{update.Type}': {update.Object.ToString(Newtonsoft.Json.Formatting.None)} ");
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
            else
            {
                return Forbid();
            }
        }
    }
}