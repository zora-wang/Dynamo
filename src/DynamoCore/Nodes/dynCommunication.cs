using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Dynamo.Models;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;
using Value = Dynamo.FScheme.Value;

namespace Dynamo.Nodes
{
    [NodeName("Web Request")]
    [NodeCategory(BuiltinNodeCategories.IO_HARDWARE)]
    [NodeDescription("Fetches data from the web using a URL.")]
    public class WebRequest : NodeWithOneOutput
    {
        public WebRequest()
        {
            InPortData.Add(new PortData("url", "A URL to query.", typeof(Value.String)));
            InPortData.Add(new PortData("interval", "How often to query (execution interval).", typeof(Value.Container)));
            OutPortData.Add(new PortData("str", "The string returned from the web request.", typeof(Value.String)));
            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            string url = ((Value.String)args[0]).Item;

            //send a webrequest to the URL
            // Initialize the WebRequest.
            var myRequest = System.Net.WebRequest.Create(url);

            // Return the response. 
            var myResponse = myRequest.GetResponse();

            var dataStream = myResponse.GetResponseStream();

            // Open the stream using a StreamReader for easy access.
            var reader = new StreamReader(dataStream);

            // Read the content.
            var responseFromServer = reader.ReadToEnd();

            reader.Close();

            // Close the response to free resources.
            myResponse.Close();

            return Value.NewString(responseFromServer);
        }

    }

    [NodeName("UDP Listener")]
    [NodeCategory(BuiltinNodeCategories.IO_HARDWARE)]
    [NodeDescription("Listens for data from the web using a UDP port")]
    public class UdpListener : NodeWithOneOutput
    {
        UdpClient listener;
        IPEndPoint groupEP;
        public string UDPResponse = "";
        int listenPort;

        public UdpListener()
        {
            InPortData.Add(new PortData("interval", "How often to query (execution interval).", typeof(Value.Number)));
            InPortData.Add(new PortData("port", "A UDP port to listen to.", typeof(object)));
            OutPortData.Add(new PortData("str", "The string returned from the web request.", typeof(Value.String)));

            RegisterAllPorts();
        }

        public class UdpState
        {
            public IPEndPoint e;
            public UdpClient u;
        }

        public static bool messageReceived = false;

        public void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var u = (UdpClient)((UdpState)(ar.AsyncState)).u;
                var e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;

                var receiveBytes = u.EndReceive(ar, ref e);
                var receiveString = Encoding.ASCII.GetString(receiveBytes);

                UDPResponse = Encoding.ASCII.GetString(receiveBytes, 0, receiveBytes.Length);
                var verboseLog = "Received broadcast from " + e.ToString() + ":\n" + UDPResponse + "\n";
                DynamoLogger.Instance.Log(verboseLog);

                Console.WriteLine("Received: {0}", receiveString);
                messageReceived = true;
            }
            catch (Exception e)
            {
                UDPResponse = "";
                DynamoLogger.Instance.Log(e.ToString());
            }
        }

        private void ListenOnUDP()
        {
            // UDP sample from http://stackoverflow.com/questions/8274247/udp-listener-respond-to-client

            if (listener == null)
            {
                listener = new UdpClient(listenPort);
                groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            }

            try
            {
                if (messageReceived != false) return;
                var s = new UdpState {e = groupEP, u = listener};

                DynamoLogger.Instance.Log("Waiting for broadcast");
                listener.BeginReceive(new AsyncCallback(ReceiveCallback), s);
            }
            catch (Exception e)
            {
                listener.Close();
                listener = null;
                messageReceived = false;
                UDPResponse = "";
                DynamoLogger.Instance.Log(e.ToString());
            }
            finally
            {
                if (messageReceived == true)
                {
                    listener.Close();
                    listener = null;
                    messageReceived = false;
                }
            }
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            listenPort = (int)((Value.Number)args[1]).Item; // udp port to listen to

            //if (((Value.Number)args[0]).Item == 1) // if exec node has pumped
            //{
            //    DispatchOnUIThread(ListenOnUDP);
            //}
            ListenOnUDP();

            return Value.NewString(UDPResponse);
        }

    }

    /// <summary>
    /// addded in response to https://github.com/ikeough/Dynamo/issues/777
    /// </summary>
    [NodeName("UDP Broadcaster")]
    [NodeCategory(BuiltinNodeCategories.IO_HARDWARE)]
    [NodeDescription("Broadcasts a message to an IP Address and port using UDP ")]
    public class UdpBroadcaster : NodeWithOneOutput
    {

        public string UDPResponse = "";
        int broadcastPort;
        string broadcastIP;
        string message;
        string status = "";

        public UdpBroadcaster()
        {
            InPortData.Add(new PortData("interval", "How often to publish (execution interval).", typeof(Value.Number)));
            InPortData.Add(new PortData("port", "A port to broadcast UDP on .", typeof(object)));
            InPortData.Add(new PortData("IP", "An IP address to broadcast to.", typeof(FScheme.Value.String), FScheme.Value.NewString("255.255.255.255")));//if no explicit IP passed in, default to broadcasting to all nodes on local subnet
            InPortData.Add(new PortData("message", "the message to broadcast.", typeof(object)));
            OutPortData.Add(new PortData("str", "The string returned from the web request.", typeof(Value.String)));

            RegisterAllPorts();
        }

         public override Value Evaluate(FSharpList<Value> args)
         {
             broadcastPort = (int)((Value.Number)args[1]).Item; // port to broadcast udp on
             broadcastIP = (string)((Value.String)args[2]).Item; // IP address to broadcast to, if no explicit IP passed in above we default to broadcasting to all nodes on local subnet
             message = (string)((Value.String)args[3]).Item; //the actual message to pump

             try
             {
                 // basic code from http://msdn.microsoft.com/en-us/library/tst0kwb1(v=vs.110).aspx
                 Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                         ProtocolType.Udp);

                 IPAddress broadcast = IPAddress.Parse(broadcastIP);

                 byte[] sendbuf = Encoding.ASCII.GetBytes(message);
                 IPEndPoint ep = new IPEndPoint(broadcast, broadcastPort);


                 s.SendTo(sendbuf, ep);
                 status = "sent UDP broadcast to " + broadcastIP + " on port " + broadcastPort;
                 DynamoLogger.Instance.Log(status);
             }
             catch (Exception e)
             {
                 DynamoLogger.Instance.Log(e.ToString());
                 status = "failed";
             }


             return Value.NewString(status);
         }
     }
}
