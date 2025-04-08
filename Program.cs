using System.Net.Sockets;
using System.Threading.Tasks;
using ipk_25_chat.Client;
using TcpClient = ipk_25_chat.Client.TcpClient;

namespace ipk_25_chat;
class Program
{
    static async Task Main(string[] args)
    {
        var argParser = new cliArgParser.CliArgParser();
        argParser.ParseCliArgs(args);
        
        if (argParser.Protocol == "tcp")
        {
            var tcpClient = new TcpClient(argParser.Server, argParser.Port);
            await tcpClient.InitClient();
        }
        
    }
}