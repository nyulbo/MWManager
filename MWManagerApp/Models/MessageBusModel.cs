using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageBus;

namespace MWManagerApp.Models
{
    public static class MessageBusConfig
    {
        public static ConnectionConfiguration Connection
        {
            get; set;
        }
        public static HostConfiguration Host
        {
            get; set;
        }
    }
}
