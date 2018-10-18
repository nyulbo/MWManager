using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;

using Newtonsoft.Json;
using MessageBus;
using MWManagerApp.Models;
using MWManagerApp.ViewModels;
using MWManagerApp.Services;
using MWManagerApp.Helpers;

namespace MWManagerApp.Views
{
    /// <summary>
    /// LogView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MWLogView : Page
    {
        private readonly int busIdx = 1;
        MWLogViewModel vm;

        public MWLogView()
        {
            InitializeComponent();
            vm = (MWLogViewModel)this.DataContext;
            vm.Config = new MWConfig
            {
                HostName = ConfigurationManager.AppSettings["HOSTNAME"],
                ID = ConfigurationManager.AppSettings["ID"],
                PW = ConfigurationManager.AppSettings["PW"]
            };
        }
        private static readonly DependencyProperty ModeProperty
                 = DependencyProperty.Register("Mode", typeof(string), typeof(Page), new PropertyMetadata("Log"));

        public string Mode
        {
            get { return (string)GetValue(ModeProperty); }
            set
            {
                SetValue(ModeProperty, value);
            }
        }
        private void SubscribeListView_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                Mode = "Subscribe";
            }
        }
        private void LogListView_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                Mode = "Log";
            }
        }
        private void GetLogs()
        {
            var lookupCondition = new LookupCondition
            {
                BeginDate = dtBeginDate.SelectedDate.Value,
                EndDate = dtEndDate.SelectedDate.Value.AddDays(1),
                Seq = (!string.IsNullOrEmpty(txtSeq.Text)) ? Convert.ToInt32(txtSeq.Text.ToString()) : 0,
                RoutingKey = txtRoutingKey.Text,
                Limit = Convert.ToInt32(txtLimit.Text.ToString())
            };
            vm.MWLogCollection = new ObservableCollection<MWLog>(new MWLogService().GetLogs(lookupCondition));
            LogListView.ItemsSource = vm.MWLogCollection;
        }
        private async void GetLogsAsync()
        {
            try
            {
                var lookupCondition = new LookupCondition
                {
                    BeginDate = dtBeginDate.SelectedDate.Value,
                    EndDate = dtEndDate.SelectedDate.Value.AddDays(1),
                    Seq = (!string.IsNullOrEmpty(txtSeq.Text)) ? Convert.ToInt32(txtSeq.Text.ToString()) : 0,
                    RoutingKey = txtRoutingKey.Text,
                    Limit = Convert.ToInt32(txtLimit.Text.ToString())
                };
                vm.MWLogCollection = new ObservableCollection<MWLog>(await new MWLogService().GetLogsAsync(lookupCondition));

                LogListView.ItemsSource = vm.MWLogCollection;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            this.GetLogsAsync();
        }
        private void btnLogListViewClear_Click(object sender, RoutedEventArgs e)
        {
            LogListView.ItemsSource = null;
        }
        private void btnSubscribeListViewClear_Click(object sender, RoutedEventArgs e)
        {
            vm.SubscribeCollection.Clear();
            SubscribeListView.ItemsSource = null;
        }
        private async void btnRun_Click(object sender, RoutedEventArgs e)
        {
            await btnRun.Dispatcher.BeginInvoke((Action)(() => {
                try
                {
                    MessageBusConfig.Connection = new ConnectionConfiguration
                    {
                        UserName = txtID.Text,
                        Password = txtPW.Text
                    };
                    MessageBusConfig.Host = new HostConfiguration
                    {
                        Host = txtHostName.Text
                    };

                    var bus = ConnectionManager.CreateBus(MessageBusConfig.Connection, MessageBusConfig.Host);
                    MWLogService.BusCollection.TryAdd(busIdx, bus);

                    var selectedQueues = vm.MWQueueCollection.Where(ps => ps.IsSelected == true);
                    if (selectedQueues.Count() <= 0)
                    {
                        string msg = "큐를 선택해 주세요";
                        MessageBox.Show(msg);
                        return;
                    }
                    foreach (MWQueue queue in selectedQueues)
                    {
                        var messageConfig = new MessageConfiguration
                        {
                            QueueName = queue.Name,
                            PrefetchCount = 50
                        };
                        new MWLogService().Subscribe((body, prop, info) => Task.Factory.StartNew(async () =>
                        {
                            var headers = prop.Headers;
                            var message = Encoding.UTF8.GetString(body);
                            DateTime nowDt = System.DateTime.Now;
                            await SubscribeListView.Dispatcher.BeginInvoke((Action)(async () =>
                            {
                                vm.SubscribeCollection.Insert(0, new MWLog
                                {
                                    Info = new ReceivedInfo
                                    {
                                        Seq = await new MWLogService().AddLogAsync(info, prop, message, nowDt),
                                        RoutingKey = info.RoutingKey,
                                        Exchange = info.Exchange,
                                        ConsumerTag = info.ConsumerTag,
                                        DeliverTag = info.DeliverTag,
                                        Redelivered = info.Redelivered,
                                        Queue = info.Queue,
                                        Payload = message,
                                        InsDate = nowDt
                                    },
                                    Prop = new ReceivedProps
                                    {   
                                        HeadersJSON = JsonConvert.SerializeObject(ModelConverter.ConvertToHeadersBase64(JsonConvert.SerializeObject(prop.Headers)), Formatting.Indented)
                                    }
                                });
                                if (vm.SubscribeCollection.Count.Equals(10))
                                    vm.SubscribeCollection.RemoveAt(vm.SubscribeCollection.Count() - 1);
                                SubscribeListView.ItemsSource = vm.SubscribeCollection.OrderByDescending(sub => sub.Info.Seq);
                            }));
                        }), busIdx, messageConfig);
                    }
                    vm.SubscribeStatus = new MWSubscribeStatus
                    {
                        Text = "실행 중",
                        IsRunning = true
                    };
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }));
            //this.Subscribe();
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {   
            vm.SubscribeStatus = new MWSubscribeStatus
            {
                Text = "중지",
                IsRunning = false
            };

            IBus bus;
            MWLogService.BusCollection.TryRemove(busIdx, out bus);
            if (bus != null)
                bus.Dispose();
        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        void GridViewColumnHeaderClickedHandler(object sender,
                                                    RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header  
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView =
              CollectionViewSource.GetDefaultView(LogListView.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
    }
}
