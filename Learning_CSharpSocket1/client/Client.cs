using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace client
{
    class Client //Synchronous client, async is much harder 
    {
        public static int Main(string[] args)
        {
            RunClient();
            Console.WriteLine("Done! Press any key to exit");
            Console.ReadKey();
            return 0;
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

                //Create TCP/IP socket
                Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                //Now we connect
                try //There's never enough try-catch
                {
                    sender.Connect(remoteEP); //Connect to the destination
                    Console.WriteLine("Socket connected to: {0}", sender.RemoteEndPoint.ToString());

                    //Encode message
                    string sMsg = DateTime.Now.ToString();
                    byte[] msg = Encoding.ASCII.GetBytes(sMsg + "<EOF>");

                    //Send the data through the socket
                    int bytesSent = sender.Send(msg);
                    
                    //Receive the response
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Incoming msg: {0}",
                        Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    //Release the socket
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                }
                catch(Exception e)
                {
                    Console.WriteLine("Inner catch: {0}", e.ToString());
                }

            }
            catch(Exception e)
            {
                Console.WriteLine("Outer catch: {0}", e.ToString());
            }
        }
    }

}
