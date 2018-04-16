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
        class ServerData
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
        private string m_CsvPatch = ".\\ServerList.csv";
        private Dictionary<int, Dictionary<string, string>> m_CsvData;
        private List<string> m_CsvHeader;
        private List<ServerData> m_ServerList;
        public MainWindow()
        {
            InitializeComponent();
            m_ServerList = new List<ServerData>();
            m_CsvData = new Dictionary<int, Dictionary<string, string>>();
            m_CsvHeader = new List<string>();
            if (LoadCsv())
                LoadData();
            Init();
        }

        private void Init()
        {
            ServerListCB.ItemsSource = m_ServerList;
            ServerListCB.SelectedValuePath = "Index";
            ServerListCB.DisplayMemberPath = "Index";
            ServerListCB.SelectedIndex = 0;
            SetServerIPAndPortTextBox();
        }

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

        Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(!client.Connected)
                {
                    string iptext = IPTB.Text;
                    string porttext = PortTB.Text;
                    IPAddress ip = IPAddress.Parse(iptext);
                    IPEndPoint point = new IPEndPoint(ip, int.Parse(porttext));
                    client.Connect(point);

                    Thread th = new Thread(ReceiveMsg)
                    {
                        IsBackground = true
                    };
                    th.Start();

                    // 如果输入栏中的ip和端口地址不在列表中，则添加
                    var finddata = m_ServerList.Find(s => s.Ip == iptext && s.Port == porttext);
                    if (finddata == null)
                    {
                        AddAndSaveCsvData(iptext, porttext);
                    }

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
                MessageBox.Show(ex.Message);
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
        
        private void ServerListCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox mCB = sender as ComboBox;
            SetServerIPAndPortTextBox();
        }

        private bool LoadCsv()
        {
            try
            {
                StreamReader sr = new StreamReader(m_CsvPatch, Encoding.Default);
                try
                {
                    var parser = new CsvParser(sr);
                    int nIndex = 1;
                    var headerrow = parser.Read();
                    if (headerrow != null)
                    {
                        foreach (string element in headerrow)
                        {
                            m_CsvHeader.Add(element);
                        }
                    }
                    while (true)
                    {
                        var row = parser.Read();
                        if (row != null)
                        {
                            Dictionary<string, string> list = new Dictionary<string, string>();
                            int i = 0;
                            foreach (string element in row)
                            {
                                list.Add(m_CsvHeader[i], element);
                                ++i;
                            }

                            m_CsvData.Add(nIndex, list);
                            ++nIndex;
                        }
                        else
                            break;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }
        }

        private void LoadData()
        {
            try
            {
                foreach (var iter in m_CsvData)
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

        private void AddAndSaveCsvData(string ip,string port)
        {
            try
            {
                // 添加数据
                m_ServerList.Add(new ServerData(ip, port));
                Dictionary<string, string> data = new Dictionary<string, string>();
                data["IP"] = ip;
                data["PORT"] = port;
                m_CsvData[m_ServerList.Count] = data;
                ServerListCB.Items.Refresh();

                StreamWriter sr = new StreamWriter(m_CsvPatch, false, Encoding.Default);
                var csv = new CsvWriter(sr);
                List<string> list = new List<string>();

                foreach (var header in m_CsvHeader)
                {
                    list.Add(header);
                }

                foreach (var csvdata in m_CsvData)
                {
                    Dictionary<string, string> value = csvdata.Value;
                    for (int i = 0; i < m_CsvHeader.Count; ++i)
                    {
                        value.TryGetValue(m_CsvHeader[i], out string ret);
                        if (ret != null)
                        {
                            list.Add(ret);
                        }
                    }
                }

                int index = 0;
                foreach (var item in list)
                {
                    csv.WriteField(item);
                    ++index;
                    if (index == m_CsvHeader.Count)
                    {
                        index = 0;
                        csv.NextRecord();
                    }
                }
                csv.Flush();
                sr.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }
    }
}
