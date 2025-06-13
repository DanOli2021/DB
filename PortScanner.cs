using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    public class PortScanner
    {
        public static int GetAvailablePort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            throw new InvalidOperationException("No available ports found in the specified range.");
        }

        public static int GetFreePort()
        {
            var port = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            port.Start();
            var portNumber = ((System.Net.IPEndPoint)port.LocalEndpoint).Port;
            port.Stop();
            return portNumber;
        }


        private static bool IsPortAvailable(int port)
        {
            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                tcpListener.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
