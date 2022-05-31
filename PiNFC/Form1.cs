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

        // UDP Client used to recieve on separate thread. May be able to entirely localise it on separate thread.
        static UdpClient udpclient;
        
        // Attempt to communicate between the threads so the network thread will join safely.
        static volatile Boolean server_running = false; // Used to control the separate server thread
        
        // local variable to keep track of the thread. 
        Thread udpthread;

        public Form1()
        {
            InitializeComponent();
        }

        // Separate function to operate on a separate thread so our GUI doesn't stop updating.
        public static void server()
        {
            Log("Starting server thread");
   
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 42069);
                udpclient = new UdpClient(RemoteIpEndPoint); // Pass in IPEndPoint to bind the socket.
                udpclient.Client.ReceiveTimeout = 1000;
                while (server_running)
                {
                    Byte[] recieveBytes;
                    try
                    {
                        recieveBytes = udpclient.Receive(ref RemoteIpEndPoint); // Listen on the bound port.
                    }catch (SocketException e)
                    {
                        Log($"Socket Exception: {e.Message}"); // C# has such a cool way to format strings
                        continue;
                    }
                    System.Diagnostics.Debug.WriteLine(Encoding.ASCII.GetString(recieveBytes, 0, recieveBytes.Length));
                    
                    // If the form is closing before we recieve the last bytes, then exit before trying to update a closed form.
                    if (server_running == false)
                    {
                        break;
                    }

                    // Must be done as the server runs on a different thread than the GUI.
                    Form1.textBox1.Invoke( (MethodInvoker) delegate {
                        Form1.textBox1.AppendText(Encoding.ASCII.GetString(recieveBytes) + Environment.NewLine);
                    });
                    
                }
                udpclient.Close();
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
            Log("Child Thread End");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            ThreadStart childref = new ThreadStart(server);
            udpthread = new Thread(childref);

            Log("Child Thread Starting");
            server_running = true;
            udpthread.Start();

        }

        // Helper method to make writing to desired output easier.
        private static void Log(String o)
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
