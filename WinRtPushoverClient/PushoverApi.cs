using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WinRtPushoverClient
{
    
    class PushoverApi
    {
        public string UserSecret
        {
            get;
            set;
        }

        public DeviceInfo Device
        {
            get;
            set;
        }

        private enum HttpMethod
        {
            Get,
            Post
        }

        private struct ApiResponse
        {
            public bool Success;
            public Dictionary<string, Object> Content;
        }

        private const string ApiUrlBase = "https://api.pushover.net/1/";

        public async Task<string> PopulateUserSecretFromServer(string username, string password)
        {
            var requestParams = new Dictionary<string, string>()
            {
                {"email", username },
                {"password", password }
            };

            var result = await this.makeApiRequest(HttpMethod.Post, "users/login.json", requestParams, true, false);
            Object secret;
            if (!result.Content.TryGetValue("secret", out secret))
            {
                Debug.WriteLine("WinRtPushoverClient: Failed to get Secret from server");
            }
            this.UserSecret = secret.ToString();
            return this.UserSecret;
        }

        public async Task<DeviceInfo> RegisterDevice(string deviceName)
        {
            var device = new DeviceInfo();
            device.Name = deviceName;
            var requestParams = new Dictionary<string, string>()
            {
                {"name", deviceName },
                {"os", "O" }
            };

            var result = await this.makeApiRequest(HttpMethod.Post, "devices.json", requestParams, true, false);
            Object id;
            if (result.Content.TryGetValue("id", out id))
            {
                device.Id = id.ToString();
            }

            return device;
        }

        public async Task<List<NotificationMessage>> GetMessages()
        {
            var notificationMessages = new List<NotificationMessage>();

            var requestParams = new Dictionary<string, string>();
            var result = await this.makeApiRequest(HttpMethod.Get, "messages.json", requestParams, true, true);

            Object messages;
            if (result.Content.TryGetValue("messages", out messages))
            {
                foreach (var message in messages as JArray)
                {
                    var msg = new NotificationMessage()
                    {
                        Application = new NotificationApplication(),
                        Message = message.SelectToken("message").ToString(),
                        Title = message.SelectToken("title").ToString(),
                        Id = message.SelectToken("id").ToString(),
                        Umid = message.SelectToken("umid").ToString()
                    };

                    notificationMessages.Add(msg);
                }
            }

            return notificationMessages;
        }

        public async Task UpdateToLastMessage(string latestMessage)
        {
            var requestParams = new Dictionary<string, string>()
            {
                { "message", latestMessage }
            };

            var result = await this.makeApiRequest(HttpMethod.Post, "devices/" + this.Device.Id + "/update_highest_message.json", requestParams, true, false);
        }

        private async Task<ApiResponse> makeApiRequest(HttpMethod method, string endpoint, Dictionary<string, string> requestParams, bool includeSecret, bool includeDeviceId)
        {
            var apiRepsonse = new ApiResponse();
            var client = new HttpClient();

            var builder = new UriBuilder(ApiUrlBase + endpoint);

            if (includeSecret)
            {
                requestParams.Add("secret", this.UserSecret);
            }

            if (includeDeviceId)
            {
                requestParams.Add("device_id", this.Device.Id);
            }

            HttpResponseMessage response;

            switch(method)
            {
                case HttpMethod.Post:
                    var content = new StringContent(ToPercentEncoding(requestParams));
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    response = await client.PostAsync(builder.ToString(), content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        apiRepsonse.Content = JsonConvert.DeserializeObject<Dictionary<string, Object>>(responseContent);
                    }
                    break;

                case HttpMethod.Get:

                    string query = "";
                    for (int i = 0; i <requestParams.Count(); i++)
                    {
                        if (i != 0)
                        {
                            query += "&";
                        }

                        query += requestParams.ElementAt(i).Key + "=" + requestParams.ElementAt(i).Value;
                    }

                    builder.Query = query;

                    response = await client.GetAsync(builder.ToString());
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        apiRepsonse.Content = JsonConvert.DeserializeObject<Dictionary<string, Object>>(responseContent);
                    }

                    break;
            }

            return apiRepsonse;
        }

        private string ToPercentEncoding(Dictionary<string, string> pairs)
        {
            List<string> joinedPairs = new List<string>();
            foreach (var pair in pairs)
            {
                joinedPairs.Add(
                    System.Net.WebUtility.UrlEncode(pair.Key) +
                    "=" +
                    System.Net.WebUtility.UrlEncode(pair.Value));
            }

            return String.Join("&", joinedPairs);
        }
    }


}
