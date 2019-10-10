//Coded by Le Vu Nguyen Khanh, October 2019
//Part of projects to learn socket programming
//This is the client part of a very simple system that enables
//two-way communication with user-input messages (like chatting)
//Does not have auto reconnection
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
        
        static bool stopFlag = false; //More like a pause flag, but whatever
        private static Thread inputThread;
        private static ManualResetEvent disconnect = new ManualResetEvent(false);
        public static int Main(string[] args)
        {
            RunClient();
            return 0;
        }

        private static bool IsConnected()
        {
            return !(curSocket.Poll(1, SelectMode.SelectRead) && curSocket.Available == 0);
        }
        //Takes care of updating the title and checking for unexpected disconnection
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
                    Console.Title = "<CLIENT>Server disconnected " + DateTime.Now.ToLongTimeString();
                    break;
                }
                if(stopFlag)
                {
                    Console.Title += " STOPPED ";
                }
                await Task.Delay(1000);
            }
            disconnect.Set();
        }
        private static void InputWork()
        {
            Console.WriteLine("Please input your message. Enter nothing to disconnect");
            while (!stopFlag)
            {
                try
                {
                    //Input message
                    string userMsg = Console.ReadLine();
                    if (string.IsNullOrEmpty(userMsg))
                    {
                        stopFlag = true;
                        userMsg = "<BYE>";
                        disconnect.Set();
                    }
                    byte[] msg = Encoding.ASCII.GetBytes(userMsg + "<EOF>");

                    //Send the data through the socket
                    int bytesSent = curSocket.Send(msg);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(DateTime.Now.ToString() + ": You: " + userMsg);
                    Console.ResetColor();
                }
                catch(Exception e)
                {
                    Console.WriteLine("Connection to server lost: " + e.Message);
                }
            }
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
        private static void OutputWork()
        {
            byte[] bytes = new byte[1024]; //Bytes received
            string data = ""; //Interpreted data
            while (!stopFlag) {
                data = "";
                try
                {
                    while (IsConnected()) //Wait why doesn't the client have this loop?
                    {
                        int bytesRec = curSocket.Receive(bytes);
                        //Console.WriteLine("Bytes fragment received");
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        //Th is wil l add up th e mes sage s slo w ly
                        int eof = data.IndexOf("<EOF>");
                        if (eof > -1)
                        {
                            //Remove the <EOF> first
                            data = data.Remove(eof, 5);
                            break; //Stop expecting message
                        }
                    }
                    if (!string.IsNullOrEmpty(data))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(DateTime.Now.ToString() + ": Server's messsage: {0}", data);
                        Console.ResetColor();
                    }
                }
                catch(Exception e)
                {
                    //Ignore
                    break;
                }
            }
        }
        private static void StartOutputListener()
        {
            Task.Run(() => OutputWork());
        }
        static async Task RunClient()
        {
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
                    StartOutputListener();
                    //Wait until we want to disconnect
                    disconnect.WaitOne();
                    //Release the socket
                    curSocket.Shutdown(SocketShutdown.Both);
                    curSocket.Close();

                }
                catch(Exception e)
                {
                    Console.WriteLine("Connection Error: {0}", e.ToString());
                }

            }
            catch(Exception e)
            {
                Console.WriteLine("Outer catch: {0}", e.ToString());
            }
        }
    }

}
