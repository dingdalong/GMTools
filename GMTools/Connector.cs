using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace GMTools
{
    public class Connector
    {
        public Connector(MainWindow window)
        {
            ParentWindow = window;
            CurrentClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// windows节点
        /// </summary>
        public MainWindow ParentWindow { set; get; }

        /// <summary>
        /// 当前Socket
        /// </summary>
        private Socket CurrentClient { set; get; }
        
        /// <summary>
        /// 接受消息线程Run标志
        /// </summary>
        private bool MsgThreadRun { set; get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected()
        {
            return CurrentClient.Connected;
        }

        /// <summary>
        /// 连接
        /// </summary>
        public bool Connect(string ip,string port)
        {
            try
            {
                IPEndPoint point = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                CurrentClient.Connect(point);

                Thread th = new Thread(ReceiveMsg)
                {
                    IsBackground = true
                };
                MsgThreadRun = true;
                th.Start();
                ParentWindow.ConnectButton.Content = "断开连接";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                DisConnect();
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void DisConnect()
        {
            try
            {
                if (CurrentClient.Connected)
                    CurrentClient.Shutdown(SocketShutdown.Both);

                CurrentClient.Close();
                CurrentClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                MsgThreadRun = false;
                ParentWindow.GetMsgFromServer("断开连接");
                ParentWindow.ConnectButton.Content = "连接";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 接受消息
        /// </summary>
        private void ReceiveMsg()
        {
            while (MsgThreadRun)
            {
                try
                {
                    if (CurrentClient != null && CurrentClient.Connected)
                    {
                        if (CurrentClient.Available <= 0)
                            continue;

                        byte[] buffer = new byte[1024 * 1024];

                        int n = CurrentClient.Receive(buffer);
                        int size = buffer[6] + buffer[7] * 256;
                        ParentWindow.GetMsgFromServer("收到消息：" + Encoding.Default.GetString(buffer, 8, size));
                    }
                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    ParentWindow.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        DisConnect();
                    }));
                }
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendMsg(string str)
        {
            try
            {
                if (CurrentClient != null && CurrentClient.Connected)
                {
                    try
                    {
                        // 消息长度（int） + 消息类型（int16） + 字符串长度（int16）
                        int TotalLength = 4 + 2 + 2;

                        // 具体消息
                        byte[] textbuffer = Encoding.Default.GetBytes(str);
                        TotalLength += textbuffer.Length;

                        byte[] totallengthbuffer = BitConverter.GetBytes(TotalLength);
                        byte[] textlengthbuffer = BitConverter.GetBytes((Int16)(textbuffer.Length));

                        byte[] buffer = new byte[1024 * 128];

                        //将消息总长度写入buffer头
                        int index = 0;
                        for (; index < totallengthbuffer.Length; ++index)
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
                        for (int i = 0; i < textbuffer.Length; ++i)
                        {
                            buffer[index] = textbuffer[i];
                            ++index;
                        }

                        CurrentClient.Send(buffer, index, SocketFlags.None);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                        DisConnect();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                DisConnect();
            }
        }
    }
}
