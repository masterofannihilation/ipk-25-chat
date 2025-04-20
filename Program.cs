using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ipk_25_chat.cliArgParser;
using ipk_25_chat.Client;

namespace ipk_25_chat;
class Program
{
    static async Task Main(string[] args)
    {
        var argParser = new CliArgParser();
        argParser.ParseCliArgs(args);
        
        switch (argParser.Protocol)
        {
            case "tcp":
                await StartTcpClient(argParser);
                break;
            case "udp":
                throw new NotImplementedException();
            default:
                Console.WriteLine("Unknown protocol. Please use 'tcp' or 'udp'");
                break;
        }
        Environment.Exit(0);
    }

    private static async Task StartTcpClient(CliArgParser argParser)
    {
        var tcpClient = new TcpChatClient(argParser.Server, argParser.Port);
        await tcpClient.RunAsync();
    }
}