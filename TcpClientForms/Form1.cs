using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace TcpClientForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private TcpClient Client;
        private const int BufferSize = 8192;
        private byte[] Buffer = new byte[BufferSize];
        private NetworkStream stream;
        private string data;
        private bool clientrun;


        //用委托解决跨线程问题
        private delegate void SetTextCallback();
        private delegate void SetReceiveTextCallback(string str);
        private SetTextCallback SetTextValue;
        private SetReceiveTextCallback SetReceiveTextValue;

        /// <summary>
        /// 发送消息到服务器
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            IPAddress serverIP = IPAddress.Parse(textBox1.Text);
            int Port = Convert.ToInt32(textBox2.Text);
            Client = new TcpClient();
            Client.BeginConnect(serverIP, Port, RequestCallback, null);//无法访问已释放的对象
            textBox3.Text += "Connecting to Server\r\n";
            SetTextValue = new SetTextCallback(SetText);
            clientrun = true;
        }

        public void RequestCallback(IAsyncResult result)
        {
            if(result!=null)
            {
                Client.EndConnect(result);
                textBox3.Invoke(SetTextValue);
                ReceiveData();
            }
        }

        /// <summary>
        /// 发送消息到服务器端
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            if(indicationstatus())
            {
                stream = Client.GetStream();
                data = textBox4.Text;
                byte[] msg = Encoding.ASCII.GetBytes(data);
                //Buffer = Encoding.ASCII.GetBytes(data);
                stream.BeginWrite(msg, 0, msg.Length, OnWriteCallback, null);
                textBox3.Text += "CToS:" + data + "\r\n";
            }
        }

        private void OnWriteCallback(IAsyncResult result)
        {
            stream.EndWrite(result);
        }

        /// <summary>
        /// 接收服务器端发送的消息
        /// </summary>
        public void ReceiveData()
        {
            if(Client.Connected&&clientrun)
            {
                stream = Client.GetStream();
                if (stream.CanRead)
                {
                    stream.BeginRead(Buffer, 0, Buffer.Length, OnReadCallback, null);
                    SetReceiveTextValue = new SetReceiveTextCallback(SetReceive);
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
        /// 客户端断开连接
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            clientrun = false;
            ClientStop();
        }

        public void ClientStop()
        {
            try
            {
                stream.Dispose();
            }
            catch(NullReferenceException)
            {
                textBox3.Text += "A1";
            }
            try
            {
                Client.Close();
            }
            catch(NullReferenceException)
            {
                textBox3.Text += "B2";
            }
            
        }

        /// <summary>
        /// 委托调用的方法
        /// </summary>
        public void SetText()
        {
            textBox3.Text += "Connection Successful\r\n";
        }

        public void SetReceive(string str)
        {
            //textBox3.Text += "SToC:" + str + "\r\n";
            textBox3.AppendText("SToC:" + str + "\r\n");
        }

        /// <summary>
        /// 指示Client Socket的状态（上一次连接的状态，不能实时）
        /// </summary>
        public bool indicationstatus()
        {
            try
            {
                return !((Client.Client.Poll(1000, SelectMode.SelectRead) && (Client.Client.Available == 0)) || (!Client.Client.Connected));
            }
            catch(ObjectDisposedException)
            {
                return false;
            }
        }
    }
}
