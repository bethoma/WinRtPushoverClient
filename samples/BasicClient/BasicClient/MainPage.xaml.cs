using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
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

            pushoverClient = new PushoverClient("WinRtClient");
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var initResult = await pushoverClient.Initialize();
            switch(initResult)
            {
                case InitializationResult.Success:
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
