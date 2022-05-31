using System;

using System.Data;
using System.Data.SqlClient;

using System.Text;
using System.Threading;

using System.Net.Sockets;
using System.Net;

using System.Windows.Forms;

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
        public static void Server()
        {
            Log("Starting server thread");

            try
            {
                // Docs say this is needed to recieve from any IP? May be useful to tighten this down to a local network/mask?
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 42069);

                udpclient = new UdpClient(RemoteIpEndPoint); // Pass in IPEndPoint to bind the socket.

                udpclient.Client.ReceiveTimeout = 1000; // timed in ms. Assuming 1 second timeout is decent for now.

                while (server_running)
                {
                    Byte[] recieveBytes; // Buffer to recieve UDP information in.

                    try
                    {
                        recieveBytes = udpclient.Receive(ref RemoteIpEndPoint); // Listen on the bound port.
                    }
                    catch (SocketException e)
                    {
                        Log($"Socket Exception: {e.Message}"); // C# has such a cool way to format strings
                        continue;
                    }

                    Log(Encoding.ASCII.GetString(recieveBytes, 0, recieveBytes.Length));

                    // If the form is closing before we recieve the last bytes, then exit before trying to update a closed form.
                    if (server_running == false)
                    {
                        break;
                    }

                    // Must be done as the server runs on a different thread than the GUI.
                    Form1.textBox1.Invoke((MethodInvoker)delegate
                    {
                        // TODO: Fix this implementation, currently relies on form1.designer.cs textbox1 being static,
                        //   which resets everytime the designer is updated.
                        Form1.textBox1.AppendText(Encoding.ASCII.GetString(recieveBytes) + Environment.NewLine);
                    });

                }
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
            finally {
                // Clean up resources
                if (udpclient != null)
                {
                    udpclient.Close(); 
                }
            }
            Log("Child Thread End");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            ThreadStart childref = new ThreadStart(Server);
            udpthread = new Thread(childref);

            Log("Child Thread Starting");
            server_running = true;
            udpthread.Start();

            Init_SQL();

        }

        // Helper method to make writing to desired output easier.
        private static void Log(String o)
        {
            System.Diagnostics.Debug.WriteLine(o);
        }

        // Make sure all threads are cleaned up before closing the program.
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            server_running = false; 
            udpthread.Join(); // Hopefully this will close after the socket RecieveTimeout passes
        }

        public static void Init_SQL()
        {
            SqlConnectionStringBuilder sqlStringBuilder = new SqlConnectionStringBuilder
            {
                // Yeah this is absolutely not the best idea
                UserID = "root",
                Password = "root",

                DataSource = "tcp:127.0.0.1,42069",
                ConnectTimeout = 1000 // 1 Second to establish connection?
            };

            Log(sqlStringBuilder.ConnectionString);

        }
    }
}
