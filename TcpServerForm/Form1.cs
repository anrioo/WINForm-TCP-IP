using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace TcpServerForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private TcpListener listener;
        private TcpClient server;
        private const int BufferSize = 8192;
        private byte[] Buffer = new byte[8192];
        private NetworkStream stream;
        private string data;
        private enum RunStatus
        {
            Running=1,
            NonRun=0
        }
        private RunStatus Running = RunStatus.Running;
        private RunStatus NonRun = RunStatus.NonRun;

        private bool serverrun = false;

        //用委托解决跨线程调用方法的问题
        private delegate void SetTextCallback();
        private delegate void SetReceiveTextCallback(string str);
        private SetTextCallback SetTextValue;
        private SetReceiveTextCallback SetReceiveTextValue;

        private void button1_Click(object sender, EventArgs e)
        {
            IPAddress IP = IPAddress.Parse(textBox2.Text);
            int Port = Convert.ToInt32(textBox1.Text);
            listener = new TcpListener(IP, Port);
            listener.Start();
            serverrun = true;
            //server = new TcpClient();
            textBox3.Text += "Server:Listener has Start, Wait for a Connection\r\n";
            SetTextValue = new SetTextCallback(SetText);
            waitClientConnect();
        }

        /// <summary>
        /// 等待客户端的连接
        /// </summary>
        public void waitClientConnect()
        {
            listener.BeginAcceptTcpClient(OnwaitCallback, null);
        }

        public void OnwaitCallback(IAsyncResult result)
        {
            if (result != null&&serverrun)//找一个连接和没连接不同的属性,可考虑通过发包检测是否仍然连接
            {
                textBox3.Invoke(SetTextValue);
                server = listener.EndAcceptTcpClient(result);
                waitClientConnect();
                ReceiveData();                 
            }
        }

        /// <summary>
        /// 服务器发送消息到客户端
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            if(indicationstatus())
            {
                stream = server.GetStream();
                data = textBox4.Text;
                byte[] msg = Encoding.ASCII.GetBytes(data);
                stream.BeginWrite(msg, 0, msg.Length, OnWriteCallback, null);
                textBox3.Text += "SToC:" + data + "\r\n";
            }
        }

        public void OnWriteCallback(IAsyncResult result)
        {
            stream.EndWrite(result);
        }

        /// <summary>
        /// 接收客户端到来的消息
        /// </summary>
        public void ReceiveData()//找一个方法在断开连接时跳出rev循环
        {
            if(server.Connected)
            {
                stream = server.GetStream();
                if (stream.CanRead)
                {
                    stream.BeginRead(Buffer, 0, Buffer.Length, OnReadCallback, null);
                    SetReceiveTextValue = new SetReceiveTextCallback(SetReceiveText);
                }
            }
        }

        public void OnReadCallback(IAsyncResult result)
        {
            if(indicationstatus())
            {
                stream.EndRead(result);
                data = Encoding.ASCII.GetString(Buffer);
                textBox3.Invoke(SetReceiveTextValue, data);
                Array.Clear(Buffer, 0, Buffer.Length);
                ReceiveData();
            }
        }

        /// <summary>
        /// 服务器断开连接
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            serverrun = false;
            ListenStop();
        }

        public void ListenStop()
        {
            try
            {
                stream.Dispose();               
            }
            catch(NullReferenceException)
            {
                textBox3.Text += "还未有流到达\r\n";
            }
            try
            {
                server.Close();
            }
            catch(NullReferenceException)
            {
                textBox3.Text += "服务器Socket未打开，不需要关闭\r\n";
            }
            listener.Stop();
        }

 /*       public void ListenStop()
        {
            if(listener!=null)
            {
                listener.Stop();
                listener = null;
                if(server.Connected)
                {
                    stream.Close();
                    server.Close();
                }
            }
        }*/

        /// <summary>
        /// 跨线程委托要调用的方法
        /// </summary>
        public void SetText()
        {
            textBox3.Text += "Server:Incoming Client\r\n";
        }

        public void SetReceiveText(string str)
        {
            //textBox3.Text += "CToS:" + str + "\r\n";
            textBox3.AppendText("CToS:" + str + "\r\n");
        }
        
        /// <summary>
        /// 指示socket连接状态(上一次连接的状态，不能实时)
        /// </summary>
        public bool indicationstatus()
        {
            try
            {
                return !((server.Client.Poll(1000, SelectMode.SelectRead) && (server.Client.Available == 0)) || !server.Client.Connected);
            }
            catch(ObjectDisposedException)
            {
                return false;
            }                      
        }//异常，server在停止监听时，已经释放，无法读取属性
    }
}
