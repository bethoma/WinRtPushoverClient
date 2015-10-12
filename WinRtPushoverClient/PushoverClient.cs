using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Credentials;
using Windows.Storage;

namespace WinRtPushoverClient
{
    public delegate void MessagesReceivedHandler(IEnumerable<NotificationMessage> messages);

    public delegate void EmergencyMessagesRecievednHandler(IEnumerable<NotificationMessage> emergencyMessages);

    public delegate void ErrorHandler(ClientError error, string message);

    //const string UserSecretResourceName = "UserSecret";

    public enum InitializationResult
    {
        Success = 0,
        AlreadyInitalized,
        AuthenticationRequired,
        Error
    }

    public enum AuthenticationResult
    {
        Success = 0,
        InvalidCredentials,
        Error
    }

    public enum ClientError
    {
        UnknownError,
        AuthenticationFailed,
        DeviceAlreadyRegistered
    }

    public sealed class PushoverClient
    {
        public event MessagesReceivedHandler MessagesReceived;

        public event EmergencyMessagesRecievednHandler EmergencyMessagesReceived;

        public event ErrorHandler OnError;

        private PushoverApi pushoverApi;

        private DeviceInfo deviceInfo;

        public PushoverClient(string deviceName)
        {
            deviceInfo = new DeviceInfo()
            {
                Name = deviceName
            };
        }

        public IAsyncOperation<InitializationResult> Initialize()
        {
            return Task.Run(() =>
            {
                if (null != pushoverApi)
                    return InitializationResult.AlreadyInitalized;

                this.pushoverApi = new PushoverApi();
                this.pushoverApi.WebSocketServerMessageRecieved += PushoverApi_WebSocketServerMessageRecieved;

                var vault = new PasswordVault();
                try
                {
                    var credential = vault.FindAllByResource("PushoverDevice_"+ this.deviceInfo.Name +" _UserSecret").FirstOrDefault();
                    if (null != credential)
                    {

                        var secret = vault.Retrieve("PushoverDevice_" + this.deviceInfo.Name + " _UserSecret", "UserSecret").Password;
                        pushoverApi.UserSecret = secret;
                    }
                }
                catch(Exception)
                {
                    return InitializationResult.AuthenticationRequired;
                }

                return InitializationResult.Success;
            }).AsAsyncOperation();
        }

        private async void PushoverApi_WebSocketServerMessageRecieved(WebSocketMessage message)
        {
            switch(message)
            {
                case WebSocketMessage.NewMessage:
                    this.MessagesReceived(await this.getMessagesAndMoveToHead());
                    break;
            }
        }

        public IAsyncOperation<AuthenticationResult> Authenticate(string username, string password)
        {
            return Task.Run(async() =>
            {
                await this.pushoverApi.PopulateUserSecretFromServer(username, password);
                var vault = new PasswordVault();
                var credential = new PasswordCredential("PushoverDevice_" + this.deviceInfo.Name + " _UserSecret", "UserSecret", this.pushoverApi.UserSecret);
                vault.Add(credential);
                return AuthenticationResult.Success;
            }).AsAsyncOperation();
        }

        public IAsyncOperation<IEnumerable<NotificationMessage>> Start()
        {
            return Task.Run(async () =>
            {
                await this.checkDeviceRegistered();
                var startupMessages = await this.pushoverApi.GetMessages();
                if (startupMessages.Any())
                {
                    startupMessages = startupMessages.OrderByDescending(m => m.Id).ToList();
                    await this.pushoverApi.UpdateToLastMessage(startupMessages[0].Id);
                }

                await this.pushoverApi.ConnectWebSocket();
                var messages = await this.getMessagesAndMoveToHead();
                return messages.AsEnumerable();
            }).AsAsyncOperation();
        }

        public IAsyncAction AcknowledgeEmergenyMessage(NotificationMessage emergencyMessage)
        {
            throw new NotImplementedException();
        }

        private async Task checkDeviceRegistered()
        {
            var settings = ApplicationData.Current.LocalSettings;
            var deviceId = settings.Values[this.deviceInfo.Name + "_DeviceId"];

            // Device needs registration
            if (null == deviceId)
            {
                this.deviceInfo = await this.pushoverApi.RegisterDevice(this.deviceInfo.Name);
                settings.Values.Add(this.deviceInfo.Name + "_DeviceId", this.deviceInfo.Id);
            }
            else
            {
                this.deviceInfo.Id = deviceId.ToString();
            }

            this.pushoverApi.Device = this.deviceInfo;
        }

        private async Task<List<NotificationMessage>> getMessagesAndMoveToHead()
        {
            var messages = await this.pushoverApi.GetMessages();
            if (messages.Any())
            {
                messages = messages.OrderByDescending(m => m.Id).ToList();
                await this.pushoverApi.UpdateToLastMessage(messages[0].Id);
            }

            return messages;
        }
    }
}
