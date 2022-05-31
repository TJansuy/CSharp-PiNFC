using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace PiNFC
{
    public partial class Form1 : Form
    {

        static UdpClient udpclient;
        static Boolean server_running = false;
        Thread udpthread;

        public Form1()
        {
            InitializeComponent();
        }

        // Separate function to operate on a separate thread so our GUI doesn't stop updating.
        public static void server()
        {
            log("Starting server thread");
   
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 42069);
                udpclient = new UdpClient(RemoteIpEndPoint); // Pass in IPEndPoint to bind the socket.
                
                while (server_running)
                {
                    log("Ping");
                    Byte[] recieveBytes = udpclient.Receive(ref RemoteIpEndPoint); // Listen on the bound port.
                    System.Diagnostics.Debug.WriteLine(Encoding.ASCII.GetString(recieveBytes, 0, recieveBytes.Length));
                    
                }
                udpclient.Close();
            }
            catch (Exception e)
            {
                log(e.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Text = "Hello World";

            ThreadStart childref = new ThreadStart(server);
            udpthread = new Thread(childref);

            log("Child Thread Starting");
            server_running = true;
            udpthread.Start();


        }

        // Helper method to make writing to desired output easier.
        private static void log(String o)
        {
            System.Diagnostics.Debug.WriteLine(o);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            server_running = false;
            udpthread.Join();
        }
    }
}
