//Coded by Le Vu Nguyen Khanh, October 2019
//Part of projects to learn socket programming
//This is a very simple server that can receive message from the client
//and send back a single message. Can also detect when a client disconnects.
//To enable the server to send user-input
//messages, it seems like I need to use asynchronous stuff.
//That will be in the next version.
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    class Server
    {
        public static int Main(string[] args)
        {
            StartListening();
            return 0;
        }
        private static async Task BackgroundRun()
        {
            while (curHandler == null || !curHandler.Connected)
            {
                Console.Title = "<SERVER>Waiting for first connection " + DateTime.Now.ToLongTimeString();
                await Task.Delay(1000);
            }
            while (true)
            {
                if (!IsClientConnected())
                    Console.Title = "<SERVER>No Connection Active " + DateTime.Now.ToLongTimeString();
                else
                    Console.Title = "<SERVER>Connection Active " + DateTime.Now.ToLongTimeString();
                await Task.Delay(1000);
            }
        }

        private static bool IsClientConnected()
        {
            return !(curHandler.Poll(1, SelectMode.SelectRead) && curHandler.Available == 0);
        }

        static Socket curHandler;
        public static string data = null; //Incoming data
        private static bool stopFlag = false;

        static void StartListening()
        {
            byte[] bytes = new byte[1024]; //Incoming bytes

            //Interesting, no try-catch needed here?

            //Establish the local endpoint for the socket 
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint localEP = new IPEndPoint(ipAddress, 11000);

            //Create TCP/IP socket
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

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

                        Console.WriteLine("A cliesnt connected");

                        while (true)
                        {
                            data = null;

                            //Process incoming connection
                            while (true && IsClientConnected()) //Wait why doesn't the client have this loop?
                            {
                                int bytesRec = curHandler.Receive(bytes);
                                //Console.WriteLine("Bytes fragment received");
                                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                                //Th is wil l add up th e mes sage s slo w ly
                                if (data.IndexOf("<EOF>") > -1)
                                {
                                    break; //Stop expecting message
                                }
                            }
                            if(!string.IsNullOrEmpty(data))
                                Console.WriteLine("Client's messsage: {0}", data);

                            //Send our own message back
                            byte[] msg = Encoding.ASCII.GetBytes(DateTime.Now.ToString() + ": ACKNOWLEDGED"); //Compose it
                            curHandler.Send(msg); //Send it
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Exception! Maybe client disconnected? {0}", e.ToString());
                    }
                    //i sleep now
                    //handler.Shutdown(SocketShutdown.Both);
                    //handler.Close();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Outer error: {0}", e.ToString());
            }

        }

    }
}
