using StoryBot.Core.Abstractions;
using System;
using VkNet.Abstractions;
using VkNet.Model.RequestParams;

namespace StoryBot.Vk.Logic
{
    public class VkMessageSender : IMessageSender<MessagesSendParams>
    {
        private readonly IVkApi vkApi;

        public VkMessageSender(IVkApi vkApi)
        {
            this.vkApi = vkApi;
        }

        public void Send(long userId, MessagesSendParams message)
        {
            message.PeerId = userId;
            message.RandomId = new DateTime().Millisecond;
            vkApi.Messages.Send(message);
        }
    }
}
