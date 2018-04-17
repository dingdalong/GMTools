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
using System.Windows.Shapes;

namespace GMTools
{
    /// <summary>
    /// ServerStatusWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ServerStatusBtn : Button
    {
        public ServerStatusBtn()
        {
            InitializeComponent();
        }
        
        public void SetName(string name)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                this.Content = name;
            }));
        }

        public void SetDisConnect()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                this.Background = Brushes.Red;
            }));
        }

        public void SetConnect()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                this.Background = Brushes.Green;
            }));
        }
    }
}
