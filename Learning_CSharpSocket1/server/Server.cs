using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace server
{
    class Server
    {
        public static int Main(string[] args)
        {
            StartListening();
            return 0;
        }

        public static string data = null; //Incoming data

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

                //Start listening 
                while (true)
                {
                    Console.WriteLine("Waiting for connection. Program will hang");
                    //bruh there's another listener
                    Socket handler = listener.Accept();
                    data = null;

                    //Process incoming connection
                    while(true) //Wait why doesn't the client have this loop?
                    {
                        int bytesRec = handler.Receive(bytes);
                        Console.WriteLine("Bytes fragment received");
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        //Th is wil l add up th e mes sage s slo w ly
                        if(data.IndexOf("<EOF>") > -1)
                        {
                            break; //Stop expecting message
                        }
                    }

                    Console.WriteLine("Final message: {0}", data);

                    //Send our own message back
                    byte[] msg = Encoding.ASCII.GetBytes("Right back at ya buckaroo"); //Compose it
                    handler.Send(msg); //Send it

                    //i sleep now
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Outer error: {0}", e.ToString());
            }

        }

    }
}
