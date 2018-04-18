using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GMTools
{
    /// <summary>
    /// BackCommand.xaml 的交互逻辑
    /// </summary>
    public partial class BackCommand : BasePage
    {
        public BackCommand()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示信息到后台信息
        /// </summary>
        public override void GetMsg(string msg)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    DateTime dt = DateTime.Now;
                    string prefix = dt.ToString() + " : ";
                    MessageTextBox.AppendText(prefix + msg + "\n");
                    MessageTextBox.ScrollToEnd();
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        private void CommandTextBox_Key(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ParentWindow.ServerConnector.SendMsg(CommandTextBox.Text);
                CommandTextBox.Text = "";
            }
        }
    }
}
