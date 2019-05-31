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

            content = content.Replace("\"", @"\""");

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = $"{{\"content\": \"{content}\"}}";
                streamWriter.Write(json);
            }

            HttpWebResponse httpResponse;

            try
            {
                httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (Exception)
            {
                return;
            }

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                streamReader.ReadToEnd();
            }
        }
    }
}
