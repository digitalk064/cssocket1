//Coded by Le Vu Nguyen Khanh, October 2019
//Part of projects to learn socket programming
//This is the server part of a very simple system that enables
//two-way communication with user-input messages (like chatting)
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace server
{
    class Server
    {
        static Socket curHandler;
        static List<Socket> sockets = new List<Socket>();
        public static string data = null; //Incoming data
        private static bool stopFlag = false;
        private static Thread inputThread;
        private static Thread outputThread;
        public static int Main(string[] args)
        {
            StartListening();
            return 0;
        }
        //Takes care of updating the title
        private static async Task BackgroundRun()
        {
            ConnectionChecker();
            while (curHandler == null || !curHandler.Connected)
            {
                Console.Title = "<SERVER>Waiting for first connection " + DateTime.Now.ToLongTimeString();
                await Task.Delay(1000);
            }
            while (true)
            {
                if (!IsClientConnected(sockets[0]))
                    Console.Title = "<SERVER>No Connection Active " + DateTime.Now.ToLongTimeString();
                else
                    Console.Title = "<SERVER>Connection Active " + DateTime.Now.ToLongTimeString();
                await Task.Delay(1000);
            }
        }
        //Frequently check if any client has disconnected
        private static async Task ConnectionChecker()
        {
            while (true)
            {
                for (int i = 0; i < sockets.Count; i++)
                {
                    if (!IsClientConnected(sockets[i]))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Removing client " + i);
                        Console.ResetColor();
                        sockets.RemoveAt(i);
                    }
                }
                await Task.Delay(500);
            }
        }
        private static void InputThread()
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
                for (int i = 0; i < sockets.Count; i++)
                {
                    try
                    {
                        int bytesSent = sockets[i].Send(msg);
                    }
                    catch(Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Removing client " + i);
                        Console.ResetColor();
                        sockets.RemoveAt(i);
                    }
                }
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
                    int bytesSent = curHandler.Send(msg);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("You: ");
                    Console.ResetColor();
                }
                inputThread.Abort();
            }
            );
            */
            inputThread = new Thread(Server.InputThread);
            inputThread.IsBackground = true;
            inputThread.Start();
        }
        private static void OutputThread()
        {
            byte[] bytes = new byte[1024]; //Incoming bytes
            while (!stopFlag)
            {
                for (int i = 0; i < sockets.Count; i++)
                {
                    data = null;
                    try
                    {
                        //Process incoming connection
                        while (IsClientConnected(sockets[i])) //Wait why doesn't the client have this loop?
                        {
                            int bytesRec = sockets[i].Receive(bytes);
                            //Console.WriteLine("Bytes fragment received");
                            data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            //Th is wil l add up th e mes sage s slo w ly
                            if (data.IndexOf("<EOF>") > -1)
                            {
                                break; //Stop expecting message
                            }
                        }
                        if (!string.IsNullOrEmpty(data))
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine("Client's messsage: {0}", data);
                            Console.ResetColor();
                        }
                    }
                    catch(Exception e)
                    {
                        //The client most likely disconnected
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Removing client " + i);
                        Console.ResetColor();
                        sockets.RemoveAt(i);
                    }
                }
            }
            outputThread.Abort();
        }
        private static void StartOutputListener()
        {
            outputThread = new Thread(Server.OutputThread);
            outputThread.IsBackground = true;
            outputThread.Start();
        }
        private static bool IsClientConnected(Socket client)
        {
            if (client == null)
                return false;
            return !(client.Poll(1, SelectMode.SelectRead) && client.Available == 0);
        }

        static void StartListening()
        {

            //Interesting, no try-catch needed here?

            //Establish the local endpoint for the socket 
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint localEP = new IPEndPoint(ipAddress, 11000);

            //Create TCP/IP socket
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            spot:
            //Now bind the socket to the local endpoint and start listening
            try
            {
                listener.Bind(localEP);
                listener.Listen(10);
                BackgroundRun();
                //Start listening 
                while (true)
                {
                    Console.WriteLine("Waiting for connection. Program will hang");
                    try
                    {
                        //bruh there's another Socket
                        curHandler = listener.Accept();
                        sockets.Add(curHandler);
                        Console.WriteLine("A client connected");
                        StartInputListener();
                        StartOutputListener();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Exception! Maybe client disconnected? {0}", e.ToString());
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Outer error: {0}", e.ToString());
                goto spot;
            }

        }

    }
}
