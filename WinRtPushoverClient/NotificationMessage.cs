using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinRtPushoverClient
{
    public enum MessagePriority
    {
        Normal,
        Emergency
    }
    public sealed class NotificationMessage
    {
        public string Id
        {
            get; set;
        }

        public string Umid
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;

        }

        public NotificationApplication Application
        {
            get;
            set;
        }

        
    }
}
