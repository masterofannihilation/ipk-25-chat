using System.Net.Sockets;
using System.Text;
using ipk_25_chat.Message;
using ipk_25_chat.Protocol;

namespace ipk_25_chat.Client;

public class TcpClient()
{
    private readonly string host;
    private readonly int _port;
    private readonly State _state;

    public TcpClient(string host, int port) : this()
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        _port = port;
        _state = new State();
    }
    public async Task InitClient()
    {
        // Establish TCP connection
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(host, _port);
        
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
                await HandleSendMessage(stream, cancellationToken, parser, input);
            }
        }
    }

    private async Task HandleSendMessage(NetworkStream stream, CancellationToken cancellationToken, ClientMsgParser parser,
        string input)
    {
        // Parse client's message and send it if it's not a help or rename command
        var type = parser.GetMsgType(input);
        
        Console.WriteLine($"Message type: {type}");
        if (_state.IsMessageTypeAllowed(type))
        {
            var formattedMessage = parser.ParseMsg(input);
            _state.HandleEvent(type);
            
            byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
            await stream.WriteAsync(data, 0, data.Length, cancellationToken);
        }
        
    }

    private async Task ReceiveMessagesAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var parser = new ServerMsgParser();
        byte[] buffer = new byte[1024];

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead > 0)
            {
                await HandleReceivedMessage(stream, cancellationToken, buffer, bytesRead, parser);
            }
        }
    }

    private async Task HandleReceivedMessage(NetworkStream stream, CancellationToken cancellationToken, byte[] buffer,
        int bytesRead, ServerMsgParser parser)
    {
        // Parse server message and print it if it's valid IPK-25-CHAT message
        var msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine(msg);
        var type = parser.GetMsgType(msg);
                
        if (_state.IsMessageTypeAllowed(type))
        {
            _state.HandleEvent(type);
            
            var formattedMsg = parser.ParseMsg(msg);
            Console.WriteLine(formattedMsg);
        }
        
        // Send ERR message to the server if malformed/unknown message received
        if (type == MessageType.Unknown)
        {
            Console.WriteLine("Malformed message received. Sending ERR message to server.");
            var formattedMsg = parser.ParseMsg(msg);
            byte[] data = Encoding.UTF8.GetBytes(formattedMsg);
            await stream.WriteAsync(data, 0,data.Length, cancellationToken);
            
        }
    }
    
}