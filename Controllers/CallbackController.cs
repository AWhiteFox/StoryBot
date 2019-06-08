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

        private readonly MessagesHandler messagesHandler;

        public CallbackController(IConfiguration configuration, IVkApi vkApi, MongoDB.Driver.IMongoDatabase database)
        {
            this.configuration = configuration;
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

            try
            {
                if (messagesHandler.GetLastMessageDate(peerId) <= content.Date)
                {
                    if (content.Text[0] == '!')
                    {
                        switch (content.Text.Remove(0, 1).ToLower())
                        {
                            case "helloworld":
                                messagesHandler.SendHelloWorld(peerId);
                                return;
                            case "reset":
                                messagesHandler.SendMenu(peerId);
                                return;
                            case "repeat":
                                messagesHandler.SendAgain(peerId);
                                return;
                        }
                    }
                    else if (content.Payload != null)
                    {
                        messagesHandler.HandleKeyboard(peerId, JsonConvert.DeserializeObject<CallbackNewMessagePayload>(content.Payload).Button);
                    }
                    else if (int.TryParse(content.Text, out int number))
                    {
                        messagesHandler.HandleNumber(peerId, number - 1);
                    }
                }
            }
            catch (Exception exception)
            {
                messagesHandler.SendError(peerId, exception.Message);
                throw;
            }
        }
    }
}