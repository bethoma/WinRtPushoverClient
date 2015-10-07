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
                        var secret = credential.Password;
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
            return Task.Run(() =>
            {
                this.pushoverApi.PopulateUserSecretFromServer(username, password);
                var vault = new PasswordVault();
                var credential = new PasswordCredential("PushoverDevice_" + this.deviceInfo.Name + " _UserSecret", "UserSecret", this.pushoverApi.UserSecret);
                vault.Add(credential);
                return AuthenticationResult.Success;
            }).AsAsyncOperation();
        }

        public IAsyncOperation<IEnumerable<NotificationMessage>> Start()
        {
            if (!isAuthenticated)
            {
                throw new InvalidOperationException("Client not authenticated, call Authenticate method prior to starting");
            }

            return Task.Run(() =>
            {
                var settings = ApplicationData.Current.LocalSettings;
                var deviceId = settings.Values[this.deviceInfo.Name + "_DeviceId"].ToString();

                // Device needs registration
                if (null == deviceId)
                {
                    this.deviceInfo = this.pushoverApi.RegisterDevice(this.deviceInfo.Name);
                }
                else
                {
                    this.deviceInfo.Id = deviceId;
                }

                this.pushoverApi.Device = this.deviceInfo;

                var startupMessages = this.pushoverApi.GetMessages();
                return startupMessages.AsEnumerable();
            }).AsAsyncOperation();
        }
    }
}
