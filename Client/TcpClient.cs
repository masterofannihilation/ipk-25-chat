using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ipk_25_chat.Message;

namespace ipk_25_chat.Client;

public class TcpClient
{
    private readonly string _host;
    private readonly int _port;
    public TcpClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task InitClient()
    {
        // Establish TCP connection
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(_host, _port);
        
        NetworkStream stream = client.GetStream();

        var cts = new CancellationTokenSource();
        var sendTask = SendMessagesAsync(stream, cts.Token);
        var receiveTask = ReceiveMessagesAsync(stream, cts.Token);

        await Task.WhenAny(sendTask, receiveTask);
    }

    private async Task SendMessagesAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var parser = new ClientMsgParser();
        while (!cancellationToken.IsCancellationRequested)
        {
            string? input = await Task.Run(() => Console.ReadLine(), cancellationToken);
            if (input != null)
            {
                // Parse client's message and send it if it's not a help or rename command
                var type = parser.GetMsgType(input);
                string formattedMessage = parser.ParseMsg(input);
                if (type != IMsgParser.MessageType.Rename && type != IMsgParser.MessageType.Help)
                {
                    byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
                    await stream.WriteAsync(data, 0, data.Length, cancellationToken);
                }
            }
        }
    }
    
    

    private async Task ReceiveMessagesAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var parser = new ServerMessageParser();
        byte[] buffer = new byte[1024];
        
        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead > 0)
            {
                // Parse server message and print it if it's valid IPK-25-CHAT message 
                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var type = parser.GetMsgType(msg);
                if (type != IMsgParser.MessageType.Unknown)
                {
                    msg = msg.Substring(0, msg.Length - 2);
                    // Remove the trailing CRLF for nice output
                    var formattedMsg = parser.ParseMsg(msg).Trim();
                    Console.WriteLine(formattedMsg);
                }
            }
        }
    }
}