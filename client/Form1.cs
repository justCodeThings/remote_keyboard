using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Diagnostics;

namespace client
{
    public partial class Form1 : Form
    {    
        int x;
        int y;
        Boolean left_click;
        Boolean right_click;
        String key;
        Boolean drag;
        Boolean mouse_up = true;

        Stopwatch left_click_watch = new Stopwatch();
        Stopwatch right_click_watch = new Stopwatch();

        // The port number for the remote device.  
        private const int port = 11002;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        public static String response = String.Empty;

        // Establish the remote endpoint for the socket.  
        public static IPAddress ipAddress = IPAddress.Parse("192.168.0.12");
        public static IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

        // Create a TCP/IP socket.  
        public static Socket client = new Socket(remoteEP.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Connect to the remote endpoint.  
            client.BeginConnect(remoteEP,
                        new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();
            this.timer1.Start();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            x = e.X;
            y = e.Y;
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                left_click = true;
                left_click_watch = Stopwatch.StartNew();
                
            }
            if(e.Button == MouseButtons.Right)
            {
                right_click = true;
                right_click_watch = Stopwatch.StartNew();
            }
        }
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
            mouse_up = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            key = "false";
            StartClient(x, y, left_click, right_click, drag, key);
            this.textBox1.Text = (x.ToString()+ y.ToString()+ left_click.ToString()+ right_click.ToString()+ key.ToString());

            // Determine whether the mouse is single clicked or drag
            if (left_click_watch.ElapsedMilliseconds > 250)
            {
                left_click = false;
                if (!mouse_up)
                {
                    left_click_watch.Stop();
                    drag = true;
                }
            }

            if (right_click_watch.ElapsedMilliseconds > 250)
            {
                right_click = false;
                right_click_watch.Stop();
            }       
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Release the socket.  
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        public static void StartClient(int x, int y, Boolean left_click, Boolean right_click, Boolean drag, String key)
        {
            // Connect to a remote device.  
            try
            {
                // Send test data to the remote device.  
                Send(client, Newtonsoft.Json.JsonConvert.SerializeObject(new { x = x.ToString(), y = y.ToString(), left_click = left_click.ToString(), right_click = right_click.ToString(), drag = drag.ToString(), key = key }));
                
                sendDone.WaitOne();

                // Receive the response from the remote device.  
                
                //Receive fuction causes gui freeze.
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console.  
                Console.WriteLine("Response received : {0}", response);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                
                //Introduce new async threading
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
    // State object for receiving data from remote device.  
    public class StateObject
    {
        // Client socket.
        
        //What is this? vvv
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
}
