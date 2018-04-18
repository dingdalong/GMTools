using System;
using System.Windows.Controls;
using System.Windows.Media;

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

        /// <summary>
        /// 设置名称
        /// </summary>
        public void SetName(string name)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                this.Content = name;
            }));
        }

        /// <summary>
        /// 设置为断开状态
        /// </summary>
        public void SetDisConnect()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                this.Background = Brushes.Red;
            }));
        }

        /// <summary>
        /// 设置为连接状态
        /// </summary>
        public void SetConnect()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                this.Background = Brushes.Green;
            }));
        }
    }
}
