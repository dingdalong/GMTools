using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

namespace GMTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(!client.Connected)
                {
                    IPAddress ip = IPAddress.Parse(IP.Text);
                    IPEndPoint point = new IPEndPoint(ip, int.Parse(Port.Text));
                    client.Connect(point);

                    Thread th = new Thread(ReceiveMsg);
                    th.IsBackground = true;
                    th.Start();

                    ConnectButton.Content = "断开连接";
                }
                else
                {
                    Destroy();
                }
            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message);
                Destroy();
            }
        }
        
        private void CommandTextBox_Key(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMsg();
            }
        }

        void ReceiveMsg()
        {
            bool Run = true;
            while (Run)
            {
                try
                {
                    if (client != null && client.Connected )
                    {
                        byte[] buffer = new byte[1024 * 1024];

                        int n = client.Receive(buffer);
                        int size = buffer[6] + buffer[7] * 256;
                        ShowMsg("收到消息：" + Encoding.Default.GetString(buffer, 8, size));
                    }
                }
                catch (Exception ex)
                {
                    ShowMsg(ex.Message);
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        Destroy();
                    }));
                    Run = false;
                }
            }
        }

        void SendMsg()
        {
            if (client != null && client.Connected)
            {
                try
                {
                    if (CommandTextBox.Text.Length > 0)
                    {
                        ShowMsg("发送消息：" + CommandTextBox.Text);

                        // 消息长度（int） + 消息类型（int16） + 字符串长度（int16）
                        int TotalLength = 4 + 2 + 2;
                    
                        // 具体消息
                        byte[] textbuffer = Encoding.Default.GetBytes(CommandTextBox.Text);
                        TotalLength += textbuffer.Length;

                        byte[] totallengthbuffer = BitConverter.GetBytes(TotalLength);
                        byte[] textlengthbuffer = BitConverter.GetBytes((Int16)(textbuffer.Length));

                        byte[] buffer = new byte[1024 * 128];

                        //将消息总长度写入buffer头
                        int index = 0;
                        for(; index < totallengthbuffer.Length;++index)
                        {
                            buffer[index] = totallengthbuffer[index];
                        }

                        // 消息类型占用2个byte位
                        index += 2;

                        // 消息长度
                        for (int i = 0; i < textlengthbuffer.Length; ++i)
                        {
                            buffer[index] = textlengthbuffer[i];
                            ++index;
                        }

                        //将消息内容写入消息
                        for (int i=0;i< textbuffer.Length;++i)
                        {
                            buffer[index] = textbuffer[i];
                            ++index;
                        }
                    
                        client.Send(buffer, index, SocketFlags.None);
                        CommandTextBox.Text = "";
                    }
                }
                catch (Exception ex)
                {
                    ShowMsg(ex.Message);
                    Destroy();
                }
            }
        }

        void ShowMsg(string msg)
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
                ShowMsg(ex.Message);
            }
        }

        void Destroy()
        {
            try
            {
                if (client.Connected) 
                    client.Shutdown(SocketShutdown.Both);

                client.Close();
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ConnectButton.Content = "连接";
            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message);
            }
        }        
    }
}
