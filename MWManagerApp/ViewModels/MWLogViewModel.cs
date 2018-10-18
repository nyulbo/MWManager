using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MWManagerApp.Models;
using MWManagerApp.Services;
using MWManagerApp.Helpers;
using System.Collections.ObjectModel;

namespace MWManagerApp.ViewModels
{
    public class MWLogViewModel : Notifier
    {
        public ObservableCollection<MWQueue> MWQueueCollection
        {
            get; set;
        }
        public ObservableCollection<MWLog> MWLogCollection
        {
            get; set;
        }
        public ObservableCollection<MWLog> SubscribeCollection { get; } = new ObservableCollection<MWLog>();

        public MWLogViewModel()
        {   
            MWQueueCollection = new ObservableCollection<MWQueue>();
            MWQueueCollection.Add(new MWQueue { Name = "trace.deliver", IsSelected = true });
            MWQueueCollection.Add(new MWQueue { Name = "trace.publish", IsSelected = true });
        }

        private MWLog mwLog;
        public MWLog MWLog
        {
            get
            {
                return mwLog;
            }
            set
            {
                mwLog = value;
                OnPropertyChanged("MWLog");
            }
        }
        private MWSubscribeStatus subscribeStatus;
        public MWSubscribeStatus SubscribeStatus
        {
            get
            {
                return subscribeStatus;
            }
            set
            {
                subscribeStatus = value;
                OnPropertyChanged("SubscribeStatus");
            }
        }
        private MWConfig config;
        public MWConfig Config
        {
            get
            {
                return config;
            }
            set
            {
                config = value;
                OnPropertyChanged("Config");
            }
        }
    }
}
