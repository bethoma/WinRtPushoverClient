using System;
using System.Collections.Generic;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRtPushoverClient;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BasicClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PushoverClient pushoverClient;
        public MainPage()
        {
            this.InitializeComponent();

            pushoverClient = new PushoverClient("WinRtClient1");
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var initResult = await pushoverClient.Initialize();
            switch(initResult)
            {
                case InitializationResult.Success:
                    this.pushoverClient.MessagesReceived += PushoverClient_NewNotification;
                    var messages = await this.pushoverClient.Start();
                    foreach (var msg in messages)
                    {
                        this.Messages.Items.Add(msg.Title);
                    }
                    break;
                case InitializationResult.AuthenticationRequired:
                    await new MessageDialog("Authentication required").ShowAsync();
                    break;
            }
        }

        private void PushoverClient_NewNotification(IEnumerable<NotificationMessage> messages)
        {
            foreach (var msg in messages)
            {
                this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (() => 
                {
                    this.Messages.Items.Add(msg.Title);
                }));
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await this.pushoverClient.Authenticate(this.userNameTextbox.Text, this.passwordTextbox.Password);
            var messages = await this.pushoverClient.Start();
            foreach(var msg in messages)
            {
                this.Messages.Items.Add(msg.Title);
            }
        }
    }
}
