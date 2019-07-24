using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Vk.Logic;
using System;

namespace StoryBot.Vk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        /// <summary>
        /// Logger
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// VK secret string
        /// </summary>
        private static readonly string secret = Environment.GetEnvironmentVariable("VK_SECRET");

        /// <summary>
        /// Events handler
        /// </summary>
        private readonly VkEventsHandler eventsHandler;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="eventsHandler"></param>
        public CallbackController(VkEventsHandler eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }

        /// <summary>
        /// HTTP POST handling
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post([FromBody] CallbackUpdate update)
        {
            if (update.Secret == secret) // VK secret string check
            {
                try // If something went wrong return Bad Request
                {
                    switch (update.Type) // Event type
                    {
                        case "confirmation":
                            return Ok(Environment.GetEnvironmentVariable("VK_CONFIRMATION"));
                        case "message_new":
                            eventsHandler.MessageNewEvent(update.Object);
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

        /// <summary>
        /// Event object of Vk Callback API
        /// </summary>
        [Serializable]
        public class CallbackUpdate
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("object")]
            public JObject Object { get; set; }

            [JsonProperty("group_id")]
            public long GroupId { get; set; }

            [JsonProperty("secret")]
            public string Secret { get; set; }
        }
    }
}