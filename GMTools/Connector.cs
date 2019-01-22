using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace GMTools
{
    public delegate void SocketConnected();
    public delegate void SocketReceiveMsg(string msg);

    public class Connector
    {
        public Connector()
        {
            CurrentClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// windows节点
        /// </summary>
        public MainWindow ParentWindow { set; get; }

        private readonly int CONNECT_TIMEOUT = 3000;
        /// <summary>
        /// 超时
        /// </summary>
        private ManualResetEvent TimeoutObject;

        public bool connected => CurrentClient != null && CurrentClient.Connected;

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

        private SocketConnected disconnected { set; get; }
        private SocketReceiveMsg receiveMsg { set; get; }

        /// <summary>
        /// 连接
        /// </summary>
        public bool Connect(string ip,string port, SocketConnected socketConnected, SocketConnected socketDisconnected, SocketReceiveMsg socketReceiveMsg)
        {
            try
            {
                disconnected = socketDisconnected;
                receiveMsg = socketReceiveMsg;
                DisConnect();

                TimeoutObject = new ManualResetEvent(false);
                IPEndPoint point = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                CurrentClient.BeginConnect(point, Connected, CurrentClient);

                TimeoutObject.Reset();
                if (TimeoutObject.WaitOne(CONNECT_TIMEOUT, false))
                {
                    ReceiveMsg();
                    socketConnected?.Invoke();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                DisConnect();
                return false;
            }
        }

        private void Connected(IAsyncResult ar)
        {
            CurrentClient.EndConnect(ar);
            TimeoutObject.Set();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void DisConnect()
        {
            try
            {
                if (IsConnected())
                {
                    disconnected?.Invoke();
                    CurrentClient.Shutdown(SocketShutdown.Both);
                }

                CurrentClient.Close();
                CurrentClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                MsgThreadRun = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private int receivePosition;
        private ProtoStream recvStream = new ProtoStream();
        private readonly Queue<byte[]> recvQueue = new Queue<byte[]>();

        /// <summary>
        /// 接受消息
        /// </summary>
        private void ReceiveMsg(IAsyncResult ar = null)
        {
            if (!connected) return;

            if (ar != null)
            {
                try
                {
                    receivePosition += CurrentClient.EndReceive(ar);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    DisConnect();
                }
            }

            int i = recvStream.Position;
            // 4个字节头
            while (receivePosition >= i + 4)
            {
                // 消息总长度
                int length = (recvStream[i]) | (recvStream[i + 1] << 8) | (recvStream[i + 2] << 16) | (recvStream[i + 3] << 24);
                int sz = length;
                if (receivePosition < i + sz)
                {
                    break;
                }

                // 去掉消息头（标识消息长度的）
                recvStream.Seek(4, SeekOrigin.Current);

                if (length > 0)
                {
                    int size = recvStream[i + 6] | (recvStream[i + 7] << 8);
                    byte[] msgData = new byte[size];
                    recvStream.Seek(4, SeekOrigin.Current);
                    recvStream.Read(msgData, 0, size);
                    
                    recvQueue.Enqueue(msgData);
                }

                i += sz;
            }

            if (receivePosition == recvStream.Buffer.Length)
            {
                recvStream.Seek(0, SeekOrigin.End);
                recvStream.MoveUp(i, i);
                receivePosition = recvStream.Position;
                recvStream.Seek(0, SeekOrigin.Begin);
            }

            try
            {
                CurrentClient.BeginReceive(recvStream.Buffer, receivePosition,
                    recvStream.Buffer.Length - receivePosition,
                    SocketFlags.None, ReceiveMsg, CurrentClient);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                DisConnect();
            }
        }

        public void Dispatch()
        {
            if(recvQueue.Count > 0)
                Parsers(recvQueue.Dequeue());
        }

        private void Parsers(byte[] msgData)
        {
            string msg = Encoding.UTF8.GetString(msgData, 0, msgData.Length);
            receiveMsg(msg);
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
                        byte[] textbuffer = Encoding.UTF8.GetBytes(str);
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
