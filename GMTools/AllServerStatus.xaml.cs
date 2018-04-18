using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace GMTools
{
    /// <summary>
    /// AllServerStatus.xaml 的交互逻辑
    /// </summary>
    public partial class AllServerStatus : BasePage
    {
        public AllServerStatus()
        {
            InitializeComponent();
            m_ServerList = new List<ServerStatus>();
        }
        
        class ServerStatus
        {
            // 服务器索引
            public string Index;
            // ip
            public string IP;
            // port
            public string Port;
            // 上次接受ping的时间
            public Int64 LastPingTime;
            // 服务器描述
            public string ServerName;
            // socket
            public Socket Sock;
            // 线程ID
            //public int ThreadId;
            // 图标
            public ServerStatusBtn ServerIcon;

            public bool IsOverTime(Int64 time)
            {
                return time - LastPingTime > 10000;
            }

            public void Close()
            {
                if (Sock != null)
                {
                    if (Sock.Connected)
                        Sock.Shutdown(SocketShutdown.Both);

                    Sock.Close();
                }

                Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                LastPingTime = 0;
            }
        }

        /// <summary>
        /// 服务器列表
        /// </summary>
        private List<ServerStatus> m_ServerList;
        
        /// <summary>
        /// 接受消息线程Run标志
        /// </summary>
        private bool MsgThreadRun { set; get; }

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        private Int64 GetNowTime()
        {
            var nowtime = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(nowtime.TotalSeconds);
        }

        /// <summary>
        /// 是否已经初始化
        /// </summary>
        private bool IsInit;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            try
            {
                if (!IsInit)
                {
                    IsInit = true;
                    foreach (var server in ParentWindow.ServerList)
                    {
                        ServerStatus serverstatus = new ServerStatus
                        {
                            Index = server.Ip + ":" + server.Port,
                            IP = server.Ip,
                            Port = server.Port,
                            Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                            ServerIcon = new ServerStatusBtn(),
                        };
                        serverstatus.ServerIcon.SetName(serverstatus.Index);
                        AllServerListWP.Children.Add(serverstatus.ServerIcon);
                        m_ServerList.Add(serverstatus);

                        Thread th = new Thread(new ParameterizedThreadStart(Run))
                        {
                            IsBackground = true
                        };
                        th.Start(serverstatus);
                    }
                }
                else
                {
                    foreach (var server in m_ServerList)
                    {
                        server.LastPingTime = 0;

                        Thread th = new Thread(new ParameterizedThreadStart(Run))
                        {
                            IsBackground = true
                        };
                        th.Start(server);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 当页签打开的时候
        /// </summary>
        public override void Open()
        {
            try
            {
                MsgThreadRun = true;
                Init();

                ParentWindow.ConnectButton.IsEnabled = false;
                ParentWindow.ServerConnector.DisConnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 当页签关闭的时候
        /// </summary>
        public override void Close()
        {
            MsgThreadRun = false;
            foreach (var server in m_ServerList)
            {
                server.Close();
            }
            ParentWindow.ConnectButton.IsEnabled = true;
        }

        /// <summary>
        /// 尝试连接
        /// </summary>
        private bool TryConnect(string ip, string port)
        {
            try
            {
                ServerStatus server = m_ServerList.Find(s => s.Index == ip + ":" + port);
                if (server != null)
                {
                    if (server.LastPingTime > 0)
                    {
                        server.Close();
                    }

                    IPEndPoint point = new IPEndPoint(IPAddress.Parse(server.IP), int.Parse(server.Port));
                    server.Sock.Connect(point);

                    Thread th = new Thread(new ParameterizedThreadStart(ReceiveMsg))
                    {
                        IsBackground = true
                    };
                    th.Start(server);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 接受消息
        /// </summary>
        private void ReceiveMsg(object obj)
        {
            while (MsgThreadRun)
            {
                ServerStatus server = obj as ServerStatus;
                try
                {
                    if (server != null && server.Sock != null && server.Sock.Connected)
                    {
                        if (server.Sock.Available <= 0)
                            continue;

                        byte[] buffer = new byte[1024 * 1024];

                        int n = server.Sock.Receive(buffer);
                        int size = buffer[6] + buffer[7] * 256;
                        string str = Encoding.Default.GetString(buffer, 8, size);
                        if (str == "__check_as_ping__")
                        {
                            server.LastPingTime = GetNowTime();
                        }
                        else
                        {
                            if (server.ServerName == null)
                            {
                                if (str == "GameServer" || str == "GameGateway")
                                {
                                    int lineid = int.Parse(server.Port) % 10;
                                    str += "(" + lineid.ToString() + "线)";
                                }
                                server.ServerName = str;
                                server.ServerIcon.SetName(str);
                            }
                        }
                    }
                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    ParentWindow.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (server != null)
                            server.Close();
                    }));
                }
            }
        }

        /// <summary>
        /// 超时检查
        /// </summary>
        void Run(object obj)
        {
            while (MsgThreadRun)
            {
                ServerStatus server = obj as ServerStatus;
                try
                {
                    if (server != null)
                    {
                        Int64 nowtime = GetNowTime();
                        if(server.IsOverTime(nowtime))
                        {
                            // 再次尝试连接
                            if (TryConnect(server.IP, server.Port))
                            {
                                server.LastPingTime = nowtime;
                                // 重新连接上的,设置为连接状态
                                server.ServerIcon.SetConnect();
                                continue;
                            }

                            // 重试后仍然未连接上的,设置为未连接状态
                            server.ServerIcon.SetDisConnect();
                        }
                    }

                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    ParentWindow.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (server != null)
                            server.Close();
                    }));
                }
            }
        }
    }
}
