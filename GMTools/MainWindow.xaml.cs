using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
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
        public class ServerData
        {
            public ServerData(string ip,string port)
            {
                Ip = ip;
                Port = port;
                Index = ip + ":" + port;
                Connected = false;
            }
            public string Index { get; set; }
            public string Ip { get; set; }
            public string Port { get; set; }
            // 是否连接
            public bool Connected { get; set; }
        }

        /// <summary>
        /// Server列表
        /// </summary>
        private List<ServerData> m_ServerList;
        public List<ServerData> ServerList
        {
            set { }
            get { return m_ServerList; }
        }

        /// <summary>
        /// 连接器
        /// </summary>
        private Connector m_Connector;
        public Connector ServerConnector
        {
            set { }
            get { return m_Connector; }
        }

        /// <summary>
        /// 配置读取
        /// </summary>
        private CsvLoader m_CsvLoader;

        /// <summary>
        /// 当前页签
        /// </summary>
        private BasePage CurrentPage;

        public MainWindow()
        {
            InitializeComponent();
            m_ServerList = new List<ServerData>();
            m_Connector = new Connector(this);
            m_CsvLoader = new CsvLoader();
            if (m_CsvLoader.LoadData())
                LoadData();
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            ServerListCB.ItemsSource = m_ServerList;
            ServerListCB.SelectedValuePath = "Index";
            ServerListCB.DisplayMemberPath = "Index";
            ServerListCB.SelectedIndex = 0;
            SetServerIPAndPortTextBox();
        }

        /// <summary>
        /// 根据Serverlist中的选择设置ip和port的TextBox
        /// </summary>
        private void SetServerIPAndPortTextBox()
        {
            if(ServerListCB.SelectedValue!=null)
            {
                var data = m_ServerList.Find(s => s.Index == ServerListCB.SelectedValue.ToString());
                if (data != null)
                {
                    IPTB.Text = data.Ip;
                    PortTB.Text = data.Port;
                }
            }
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        void Destroy()
        {
            try
            {
                if(m_Connector.IsConnected())
                {
                    m_Connector.DisConnect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        
        /// <summary>
        /// 将CSV中读取的数据加载到ServerList中
        /// </summary>
        private void LoadData()
        {
            try
            {
                foreach (var iter in m_CsvLoader.CsvData)
                {
                    var data = iter.Value;
                    m_ServerList.Add(new ServerData(data["IP"], data["PORT"]));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        /// <summary>
        /// 将ip和port添加到serverlist，并保存到csv文件中
        /// </summary>
        private void AddAndSaveCsvData(string ip,string port)
        {
            try
            {
                m_ServerList.Add(new ServerData(ip, port));
                ServerListCB.Items.Refresh();
                m_CsvLoader.AddData(ip, port,m_ServerList.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        public void GetMsgFromServer(string str)
        {
            CurrentPage.GetMsg(str);
        }

        /// <summary>
        /// ServerList选项改变时
        /// </summary>
        private void ServerListCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox mCB = sender as ComboBox;
            SetServerIPAndPortTextBox();
            Destroy();
        }
        
        /// <summary>
        /// 连接按钮
        /// </summary>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string iptext = IPTB.Text;
                string porttext = PortTB.Text;

                if (ConnectButton.Content.ToString() == "连接")
                {
                    if (m_Connector.Connect(iptext, porttext))
                    {
                        // 如果输入栏中的ip和端口地址不在列表中，则添加
                        var finddata = m_ServerList.Find(s => s.Ip == iptext && s.Port == porttext);
                        if (finddata == null)
                        {
                            AddAndSaveCsvData(iptext, porttext);
                        }
                        else
                            finddata.Connected = true;

                        ConnectButton.Content = "断开连接";
                    }
                }
                else
                {
                    Destroy();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Destroy();
            }
        }

        /// <summary>
        /// 页签的改变
        /// </summary>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tc = sender as TabControl;

            if (CurrentPage != null)
                CurrentPage.Close();

            var item = tc.SelectedItem as TabItem;
            var frame = (item.Content as Frame);

            CurrentPage = frame.Content as BasePage;
            if (CurrentPage != null)
            {
                CurrentPage.ParentWindow = this;
                CurrentPage.Open();
            }
        }
    }
}
