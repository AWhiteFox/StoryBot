using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace StoryBot.Logging
{
    public class DiscordWebhook
    {
        private readonly string webhookUrl;

        public DiscordWebhook(string id, string token)
        {
            webhookUrl = $"https://discordapp.com/api/webhooks/{id}/{token}";
        }

        public void Send(string content)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(webhookUrl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(JsonConvert.SerializeObject(new Json
                {
                    Content = content
                }));
            }

            HttpWebResponse httpResponse;

            try
            {
                httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch
            {
                return;
            }

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                streamReader.ReadToEnd();
            }
        }

        [Serializable]
        private class Json
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }
    }
}
