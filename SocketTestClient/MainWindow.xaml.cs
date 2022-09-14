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

namespace SocketTestClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Socket _socket;
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var exitString = "退出服务";
            ServerExit(exitString, _socket);
        }

        private void bt_Start_Click(object sender, RoutedEventArgs e)
        {
            //连接Socket，启动监听定时器
            _socket = CreateSocket(tb_IPValue.Text, tb_PortValue.Text);
            CreateRecThread(_socket);


        }

        public Socket CreateSocket(string ip,string port)
        {
            //1、创建Socket对象,IP4寻址协议,流式连接,TCP协议
            Socket socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //2.绑定IP 和 端口
            IPAddress ipAddress = IPAddress.Parse(ip);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, int.Parse(port));

            socketClient.Connect(endPoint);
            return socketClient;
        }

        public void CreateRecThread(Socket socket)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveServerMsg), socket);
        }

        public void ReceiveServerMsg(object socketClient)
        {
            var portSocket = socketClient as Socket;
            //创建缓存内存，存储接收的信息   ,不能放到while中，这块内存可以循环利用
            byte[] data = new byte[1024 * 1024];
            while (true)
            {
                int len=0;
                try
                {
                    len = portSocket.Receive(data,0,data.Length,SocketFlags.None);
                }catch(Exception ex)
                {
                    //7、关闭Socket
                    //异常退出
                    break;
                }
                if(len <= 0)
                {
                    break;
                }
                string msgRec = Encoding.Default.GetString(data, 0, len);
                //写入到RichTextBox
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.rtb_Log.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n");
                    this.rtb_Log.AppendText(msgRec + "\r\n");
                }));
            }
        }

        private void bt_Send_Click(object sender, RoutedEventArgs e)
        {
            byte[] sendData = Encoding.Default.GetBytes(tb_Message.Text);
            _socket.Send(sendData);
        }

        public void ServerExit(string msg,Socket threadSocket)
        {
            try
            {
                if (threadSocket.Connected)
                {
                    byte[] sendMsg = Encoding.Default.GetBytes(msg);
                    threadSocket.Send(sendMsg);
                    threadSocket.Shutdown(SocketShutdown.Both);
                    threadSocket.Close(100);
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
