using System.Net.Sockets;
using System.Text;
using ipk_25_chat.Message;
using ipk_25_chat.Protocol;

namespace ipk_25_chat.Client;

public class TcpClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly State _state;

    public TcpClient(string host, int port)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _port = port;
        _state = new State();
    }
    public async Task InitClient()
    {
        // Establish TCP connection
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(_host, _port);
        
        NetworkStream stream = client.GetStream();

        var cts = new CancellationTokenSource();
        
        // Handle CTRL+C
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            await Disconnect(stream, cts);
            Environment.Exit(0);
        };
        
        var sendTask = SendMessagesAsync(stream, cts);
        var receiveTask = ReceiveMessagesAsync(stream, cts);

        await Task.WhenAny(sendTask, receiveTask);
    }

    private async Task SendMessagesAsync(NetworkStream stream, CancellationTokenSource cts)
    {
        var parser = new ClientMsgParser();

        while (!cts.IsCancellationRequested)
        {
            string? input = await Task.Run(() => Console.ReadLine(), cts.Token);
            if (input != null)
            {
                await HandleSendMessage(stream, cts, parser, input);
            }
            else
            {
                // Handle null input (EOF or Ctrl+D)
                // Send BYE message and gracefully disconnect
                var displayName = parser.GetDisplayName() ?? "Unknown";
                byte[] data = Encoding.UTF8.GetBytes($"BYE FROM {displayName}\r\n");
                await stream.WriteAsync(data, 0, data.Length, cts.Token);
                await Disconnect(stream, cts);
            }
        }
    }

    private async Task HandleSendMessage(NetworkStream stream, CancellationTokenSource cts, ClientMsgParser parser,
        string input)
    {
        // Parse client's message and send it if it's not a help or rename command
        var type = parser.GetMsgType(input);
        
        Console.WriteLine($"Message type: {type}");
        if (_state.IsMessageTypeAllowed(type))
        {
            var formattedMessage = parser.ParseMsg(input);
            
            _state.HandleEvent(type);
            
            if (_state.CurrentState == StateType.End)
                await Disconnect(stream, cts);

            byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
            await stream.WriteAsync(data, 0, data.Length, cts.Token);
        }
        
    }

    private async Task ReceiveMessagesAsync(NetworkStream stream, CancellationTokenSource cts)
    {
        var parser = new ServerMsgParser();
        byte[] buffer = new byte[1024];

        while (!cts.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            if (bytesRead > 0)
            {
                await HandleReceivedMessage(stream, cts, buffer, bytesRead, parser);
            }
        }
    }

    private async Task HandleReceivedMessage(NetworkStream stream, CancellationTokenSource cts, byte[] buffer,
        int bytesRead, ServerMsgParser parser)
    {
        // Parse server message and print it if it's valid IPK-25-CHAT message
        var msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        var type = parser.GetMsgType(msg);
                
        if (_state.IsMessageTypeAllowed(type))
        {
            _state.HandleEvent(type);
            
            var formattedMsg = parser.ParseMsg(msg);
            if (formattedMsg != "" && formattedMsg != "ERROR")
                Console.WriteLine(formattedMsg);
            
            if (formattedMsg == "ERROR")
                await Disconnect(stream, cts);
            
            if (_state.CurrentState == StateType.End)
                await Disconnect(stream, cts);
        }
        await HandleMalformedMessage(stream, cts, parser, type, msg);
    }

    private async Task HandleMalformedMessage(NetworkStream stream, CancellationTokenSource cts,
        ServerMsgParser parser, MessageType type, string msg)
    {
        // Send ERR message to the server if malformed/unknown message received
        if (type == MessageType.Unknown)
        {
            Console.Write($"ERROR: {msg}");
            var displayName = parser.GetDisplayName() ?? "Unknown";
            byte[] data = Encoding.UTF8.GetBytes($"ERR FROM {displayName}\r\n");
            await stream.WriteAsync(data, 0, data.Length, cts.Token);
            await Disconnect(stream, cts);
        }
    }

    private async Task Disconnect(NetworkStream stream, CancellationTokenSource cts)
    {
        try
        {
            Console.WriteLine("Disconnecting from the server...");
        
            cts.Cancel();

            await stream.FlushAsync();
            stream.Close();

            Console.WriteLine("Disconnected successfully.");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during disconnection: {ex.Message}");
        }
    }
}