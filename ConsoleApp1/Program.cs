using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using RemotingService;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host;
            NetTcpBinding tcp = new NetTcpBinding();
            host = new ServiceHost(typeof(Remoting));
            host.AddServiceEndpoint(typeof(RemotingInterface), tcp, "net.tcp://" + "localhost" + ":" + "8200" + "/RemotingService");
            host.Open();
            Console.ReadLine();
        }
    }
}
