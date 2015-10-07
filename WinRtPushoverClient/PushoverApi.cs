using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        public string PopulateUserSecretFromServer(string username, string password)
        {
            return "";
        }

        public DeviceInfo RegisterDevice(string deviceName)
        {
            var device = new DeviceInfo();
            device.Name = deviceName;
            var requestParams = new Dictionary<string, string>();
            var result = this.makeApiRequest(HttpMethod.Post, "devices.json", requestParams, true, false);
            string id;
            if (result.TryGetValue("id", out id))
            {
                device.Id = id;
            }

            return device;
        }

        public List<NotificationMessage> GetMessages()
        {
            var messages = new List<NotificationMessage>();

            return messages;
        }

        public void DeleteMessages(int latestMessage)
        {

        }

        private Dictionary<string, string> makeApiRequest(HttpMethod method, string endpoint, Dictionary<string, string> requestParams, bool includeSecret, bool includeDeviceId)
        {
            var result = new Dictionary<string, string>();



            return result;
        }
    }


}
