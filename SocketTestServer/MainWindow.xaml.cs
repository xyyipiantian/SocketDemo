using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace SocketTestServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Socket _socket;
        public Dictionary<string, Socket> clinets = new Dictionary<string, Socket>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void bt_Start_Click(object sender, RoutedEventArgs e)
        {
            //创建Socket服务
            _socket = CreateSocket(tb_IPValue.Text, tb_PortValue.Text);
            //创建监听进程
            rtb_Log.AppendText("启动服务成功\r\n");
            //4、开始接受客户端的连接  ，连接会阻塞主线程，故使用线程池。
            ThreadPool.QueueUserWorkItem(new WaitCallback(AcceptClientContent), _socket);

        }

        public Socket CreateSocket(string ip,string port)
        {
            try
            {
                Socket socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPAddress ipAddress = IPAddress.Parse(ip);

                IPEndPoint endPoint = new IPEndPoint(ipAddress, int.Parse(port));

                socketServer.Bind(endPoint);

                socketServer.Listen(20);

                return socketServer;
            }catch(Exception ex)
            {
                throw new Exception("Socket启动失败" + ex.Message);
            }
        }

        public void AcceptClientContent(object obj)
        {
            Socket socketServer = obj as Socket;
            while (true)
            {
                Socket socConnection = null;
                try
                {
                    socConnection = socketServer.Accept();
                    string acceptIP = ((IPEndPoint)socConnection.RemoteEndPoint).Address.ToString();
                    string acceptPort = ((IPEndPoint)socConnection.RemoteEndPoint).Port.ToString();
                    string keyValue = acceptIP + ":" + acceptPort;
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        cb_Users.Items.Add(keyValue);
                        rtb_Log.AppendText("客户端" + keyValue + "连接成功");
                    }));
                    clinets[keyValue] = socConnection;
                }
                catch(Exception ex)
                {
                    break;
                }

                //创建通讯线程
                ThreadPool.QueueUserWorkItem(new WaitCallback(ServerRecMsg), socConnection);
            }

        }

        private void ServerRecMsg(object obj)
        {
            Socket accpetSocket = obj as Socket;
            //创建缓存内存，存储接收的信息   ,不能放到while中，这块内存可以循环利用
            byte[] buffer = new byte[1020 * 1024];
            string ip = ((IPEndPoint)accpetSocket.RemoteEndPoint).Address.ToString();//获取客户端IP
            string port = ((IPEndPoint)accpetSocket.RemoteEndPoint).Port.ToString();//获取客户端端口
            string client = ip + ":" + port;
            while (true)
            {
                int firstReceived = 0;
                try
                {
                    //获取接收的数据,并存入内存缓冲区  返回一个字节数组的长度
                    if (accpetSocket != null)
                    {
                        firstReceived = accpetSocket.Receive(buffer);
                       
                    }
                    if (firstReceived > 0)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() => {
                            rtb_Log.AppendText(client+"    "+DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "\r\n");
                            var msgRec = Encoding.Default.GetString(buffer,0,firstReceived);
                            rtb_Log.AppendText(msgRec + "  \r\n");
                        }));
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(new Action(() => {
                            if (cb_Users.Items.Contains(client))
                            {
                                cb_Users.Items.Remove(client);
                            }
                        }));
                        break;
                    }
                }
                catch (Exception ex)
                {
                    this.Dispatcher.BeginInvoke(new Action(()=>{
                        if (cb_Users.Items.Contains(client))
                        {
                            cb_Users.Items.Remove(client);
                        }
                    }));
                    break;
                }
            }

        }

        private void bt_Send_Click(object sender, RoutedEventArgs e)
        {
            if (clinets.ContainsKey(cb_Users.Text))
            {
                byte[] sendDate = Encoding.Default.GetBytes(tb_Message.Text);
                clinets[cb_Users.Text].Send(sendDate);
            }
        }
    }
}
