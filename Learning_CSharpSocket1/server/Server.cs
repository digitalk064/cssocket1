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
        static List<Task> tasks = new List<Task>();
        private static bool stopFlag = false;
        private static Thread inputThread;
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
                /*
                if (!IsClientConnected(sockets[0]))
                    Console.Title = "<SERVER>No Connection Active " + DateTime.Now.ToLongTimeString();
                else
                */
                Console.Title = String.Format("<SERVER>{0} Connections, {1} Tasks, {2}",sockets.Count, tasks.Count, DateTime.Now.ToLongTimeString());
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
                        tasks.RemoveAt(i);
                        sockets.RemoveAt(i);
                    }
                }
                await Task.Delay(200);
            }
        }
        private static void InputThread()
        {
            while (!stopFlag)
            {
                string userMsg = Console.ReadLine();
                if (string.IsNullOrEmpty(userMsg))
                {
                    stopFlag = true;
                    userMsg = "<BYE>";
                }
                byte[] msg = Encoding.ASCII.GetBytes(userMsg + "<EOF>");
                //Send the data through the socket
                for (int i = 0; i < sockets.Count; i++)
                {
                    try
                    {
                        int bytesSent = sockets[i].Send(msg);
                    }
                    catch(Exception e)
                    {
                        //The client most likely disconnected
                    }
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("You: " + userMsg);
                Console.ResetColor();
            }
            //inputThread.Abort();
        }
        private static void StartInputListener()
        {
            inputThread = new Thread(Server.InputThread);
            inputThread.IsBackground = true;
            inputThread.Start();
        }
        private static void OutputThread(Socket soc, string IP)
        {
            byte[] bytes = new byte[1024]; //Incoming bytes
            string data; //Interpreted data
            while (!stopFlag)
            {
                data = null;
                try
                {
                    //Process incoming connection
                    while (IsClientConnected(soc)) //Wait why doesn't the client have this loop?
                    {
                        int bytesRec = soc.Receive(bytes);
                        //Console.WriteLine("Bytes fragment received");
                        //Th is wil l add up th e mes sage s slo w ly
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<BYE>") == 0)
                        {
                            throw new Exception("Deliberate disconnection");
                        }
                        int eof = data.IndexOf("<EOF>");
                        if (eof > -1)
                        {
                            //Remove the <EOF> first
                            data=data.Remove(eof, 5);
                            break; //Stop expecting message
                        }
                    }
                    if (!string.IsNullOrEmpty(data))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(DateTime.Now.ToString() + ": Client {0} messsage: {1}", IP, data);
                        Console.ResetColor();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("A client disconnected: " + e.Message);
                    break;
                    //Ignore and stop processing this socket
                }
            }
        }
        private static void StartOutputListener(Socket soc)
        {
            /*
            Thread outputThread = new Thread(() => Server.OutputThread(soc));
            outputThread.IsBackground = true;
            outputThread.Start();
            */
            //Get IP
            string ip = (soc.RemoteEndPoint as IPEndPoint).Address.ToString();
            tasks.Add(Task.Run(() => Server.OutputThread(soc,ip)));
        }
        private static bool IsClientConnected(Socket client)
        {
            if (client == null)
                return false;
            return !(client.Poll(1, SelectMode.SelectRead) && client.Available == 0);
        }
        private static int HandleNewSocket(Socket soc)
        {
            sockets.Add(soc);
            StartOutputListener(soc);
            curHandler.Send(Encoding.ASCII.GetBytes("You are client " + (sockets.Count - 1) + "<EOF>"));
            return sockets.Count-1;
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
                Console.WriteLine("Waiting for connection... The program will hang until then");
                //Start listening 
                while (true)
                {
                    try
                    {
                        //bruh there's another Socket
                        curHandler = listener.Accept();
                        int index = HandleNewSocket(curHandler);
                        Console.WriteLine("A client connected from " + (curHandler.RemoteEndPoint as IPEndPoint).Address);
                        StartInputListener();
                        //StartOutputListener(sockets.Count-1);
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
