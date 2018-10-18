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

using MWManagerApp.Services;

namespace MWManagerApp
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //e.Cancel = false;
            foreach (var c in MWLogService.BusCollection)
            {
                if (c.Value != null) c.Value.Dispose();
            }
            Environment.Exit(0);
            //Application.Current.Shutdown();
            //System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
