using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryBot.Messaging;
using StoryBot.Model;
using System;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Utils;

namespace StoryBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IConfiguration configuration;

        private readonly MessageHandler messageHandler;

        public CallbackController(IConfiguration configuration, IVkApi vkApi, MongoDB.Driver.IMongoDatabase database)
        {
            this.configuration = configuration;
            messageHandler = new MessageHandler(vkApi,
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
                        return Ok(configuration["Config:Confirmation"]);
                    case "message_new":
                        NewMessage(update.Object);
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

        private void NewMessage(JObject obj)
        {
            var content = Message.FromJson(new VkResponse(obj));
            long peerId = content.PeerId.Value;

            if (messageHandler.GetLastMessageDate(peerId) <= content.Date)
            {
                if (content.Text[0] == '!')
                {
                    switch (content.Text.Remove(0, 1).ToLower())
                    {
                        case "helloworld":
                            messageHandler.SendHelloWorld(peerId);
                            return;
                        case "reset":
                            messageHandler.SendMenu(peerId);
                            return;
                    }
                }
                else if (content.Payload != null)
                {
                    messageHandler.HandleKeyboard(peerId, JsonConvert.DeserializeObject<CallbackNewMessagePayload>(content.Payload).Button);
                }
                else if (int.TryParse(content.Text, out int number))
                {
                    messageHandler.HandleNumber(peerId, number - 1);
                }
            }
            return;
        }
    }
}