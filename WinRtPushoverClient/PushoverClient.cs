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
    public delegate void NewNotificationHandler(NotificationMessage message);

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

    public sealed class PushoverClient
    {
        /// <summary>
        /// Gets the current volume
        /// </summary>
        public event NewNotificationHandler NewNotification;

        private bool isAuthenticated;

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

                pushoverApi = new PushoverApi();

                var vault = new PasswordVault();
                try
                {
                    var credential = vault.FindAllByResource("PushoverDevice_"+ this.deviceInfo.Name +" _UserSecret").FirstOrDefault();
                    if (null != credential)
                    {

                        var secret = vault.Retrieve("PushoverDevice_" + this.deviceInfo.Name + " _UserSecret", "UserSecret").Password;
                        pushoverApi.UserSecret = secret;
                    }
                    isAuthenticated = true;
                }
                catch(Exception)
                {
                    isAuthenticated = false;
                    return InitializationResult.AuthenticationRequired;
                }

                return InitializationResult.Success;
            }).AsAsyncOperation();
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

                return startupMessages.AsEnumerable();
            }).AsAsyncOperation();
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
    }
}
