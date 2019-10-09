//Coded by Le Vu Nguyen Khanh, October 2019
//Part of projects to learn socket programming
//This is the client part of a very simple system that enables
//two-way communication with user-input messages (like chatting)
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace client
{
    class Client //Synchronous client, async is much harder 
    {
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

        static Socket curSocket;
        
        static bool stopFlag = false;
        private static Thread inputThread;

        public static int Main(string[] args)
        {
            RunClient();
            return 0;
        }

        private static bool IsConnected()
        {
            return !(curSocket.Poll(1, SelectMode.SelectRead) && curSocket.Available == 0);
        }
        private static async Task BackgroundRun()
        {
            while(!curSocket.Connected)
            {
                Console.Title = "<CLIENT>No Connection " + DateTime.Now.ToLongTimeString();
                await Task.Delay(1000);
            }
            while (true) { 
                Console.Title = "<CLIENT>Connection Active " + DateTime.Now.ToLongTimeString();
                if (!IsConnected()) {
                    var handle = GetStdHandle(STD_INPUT_HANDLE);
                    CancelIoEx(handle, IntPtr.Zero);
                    break;
                }
                if(stopFlag)
                {
                    break;
                }
                await Task.Delay(1000);
            }
        }
        private static void InputWork()
        {
            while (!stopFlag)
            {
                //Encode message
                string sMsg = DateTime.Now.ToString();

                Console.WriteLine("Please input your message. Enter nothing to disconnect");
                string userMsg = Console.ReadLine();
                if (string.IsNullOrEmpty(userMsg))
                {
                    stopFlag = true;
                    userMsg = "<Goodbye message>";
                }

                sMsg += ": " + userMsg;
                byte[] msg = Encoding.ASCII.GetBytes(sMsg + "<EOF>");

                //Send the data through the socket
                int bytesSent = curSocket.Send(msg);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("You: " + sMsg);
                Console.ResetColor();
            }
            inputThread.Abort();
        }
        private static void StartInputListener()
        {
            /*
            inputThread = new Thread(() =>
            {
                while (!stopFlag)
                {
                    //Encode message
                    string sMsg = DateTime.Now.ToString();

                    Console.WriteLine("Please input your message. Enter nothing to disconnect");
                    string userMsg = Console.ReadLine();
                    if (string.IsNullOrEmpty(userMsg))
                    {
                        stopFlag = true;
                        userMsg = "<Goodbye message>";
                    }

                    sMsg += ": " + userMsg;
                    byte[] msg = Encoding.ASCII.GetBytes(sMsg + "<EOF>");

                    //Send the data through the socket
                    int bytesSent = curSocket.Send(msg);
                }
                inputThread.Abort();
            }
            );*/
            inputThread = new Thread(Client.InputWork);
            inputThread.IsBackground = true;
            inputThread.Start();
        }

        static void RunClient()
        {
            byte[] bytes = new byte[1024]; //Bytes received
            try
            {
                //Most of the code here is copied from Microsoft lol
                //Get the remote endpoint (in this case, ourselves) for the socket
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                //Does this work?
                //IPAddress ipAddr = Dns.GetHostAddresses(Dns.GetHostName())[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddr, 11000); //Use port 11000 just cause

                //Init our TCP/IP socket
                curSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                //Now we connect
                try //There's never enough try-catch
                {
                    BackgroundRun();

                    curSocket.Connect(remoteEP); //Connect to the destination
                    Console.WriteLine("Socket connected to: {0}", curSocket.RemoteEndPoint.ToString());
                    StartInputListener();
                    while (!stopFlag)
                    {
                        //Receive the response
                        int bytesRec = curSocket.Receive(bytes);
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("Server's message: {0}",
                            Encoding.ASCII.GetString(bytes, 0, bytesRec));
                        Console.ResetColor();
                    }
                    //Release the socket
                    curSocket.Shutdown(SocketShutdown.Both);
                    curSocket.Close();

                }
                catch(Exception e)
                {
                    Console.WriteLine("Unexpected disconnection: {0}", e.ToString());
                }

            }
            catch(Exception e)
            {
                Console.WriteLine("Outer catch: {0}", e.ToString());
            }
        }
    }

}
