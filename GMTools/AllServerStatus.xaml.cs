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
            public Connector Connector;
            // 图标
            public ServerStatusBtn ServerIcon;

            public bool IsOverTime(Int64 time)
            {
                return LastPingTime > 0 && time - LastPingTime > 1;
            }

            public void Close()
            {
                if (Connector != null)
                {
                    if (Connector.IsConnected())
                        Connector.DisConnect();
                }
                
                LastPingTime = 0;
                ServerIcon.SetDisConnect();
            }

            public void Connected()
            {
                LastPingTime = GetNowTime();
                // 重新连接上的,设置为连接状态
                ServerIcon.SetConnect();
            }

            public void Disconnected()
            {
                ServerIcon.SetDisConnect();
            }
            public void ReceiveMsg(string str)
            {
                try
                {
                    if (str == "__check_as_ping__")
                    {
                        LastPingTime = GetNowTime();
                    }
                    else
                    {
                        if (ServerName == null)
                        {
                            if (str == "GameServer" || str == "GameGateway")
                            {
                                int lineid = int.Parse(Port) % 10;
                                str += "(" + lineid.ToString() + "线)";
                            }
                            ServerName = str;
                            ServerIcon.SetName(str);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    Close();
                }
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
        public static Int64 GetNowTime()
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
                            ServerIcon = new ServerStatusBtn(),
                        };
                        serverstatus.Connector = new Connector();
                        serverstatus.ServerIcon.SetName(serverstatus.Index);
                        AllServerListWP.Children.Add(serverstatus.ServerIcon);
                        m_ServerList.Add(serverstatus);
                    }

                    Thread th = new Thread(Run)
                    {
                        IsBackground = true
                    };
                    th.Start();
                }
                else
                {
                    foreach (var server in m_ServerList)
                    {
                        server.LastPingTime = 0;
                    }

                    Thread th = new Thread(Run)
                    {
                        IsBackground = true
                    };
                    th.Start();
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
        /// 超时检查
        /// </summary>
        void Run()
        {
            while (MsgThreadRun)
            {
                foreach (var server in m_ServerList)
                {
                    try
                    {
                        if (server != null)
                        {
                            Int64 nowtime = GetNowTime();
                            if(server.IsOverTime(nowtime) || !server.Connector.IsConnected())
                            {
                                // 再次尝试连接
                                if (server.Connector.Connect(server.IP, server.Port, server.Connected, server.Disconnected,
                                    server.ReceiveMsg))
                                {
                                    continue;
                                }

                                // 重试后仍然未连接上的,设置为未连接状态
                                server.ServerIcon.SetDisConnect();
                            }
                            server.Connector.Dispatch();
                        }
                        Thread.Sleep(1);
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
}
