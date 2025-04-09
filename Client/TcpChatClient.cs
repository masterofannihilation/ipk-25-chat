using System.Net.Sockets;
using System.Text;
using ipk_25_chat.Message;
using ipk_25_chat.Protocol;

namespace ipk_25_chat.Client;

public class TcpChatClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly State _state = new();
    private string _displayName = "Unknown";
    private const int BufferSize = 1024;

    public TcpChatClient(string host, int port)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _port = port;
    }

    public async Task RunAsync()
    {
        // Establish TCP connection
        using var client = new TcpClient();
        await client.ConnectAsync(_host, _port);
        
        
        NetworkStream stream = client.GetStream();
        var cts = new CancellationTokenSource();
        
        RegisterCancelKeyPress(stream, cts);
        
        var sendTask = ProcessOutgoingMessagesAsync(stream, cts);
        var receiveTask = ProcessIncomingMessagesAsync(stream, cts);

        await Task.WhenAny(sendTask, receiveTask);
    }
    
    private void RegisterCancelKeyPress(NetworkStream stream, CancellationTokenSource cts)
    {
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            await Disconnect(stream, cts);
            Environment.Exit(0);
        };
    }

    private async Task ProcessOutgoingMessagesAsync(NetworkStream stream, CancellationTokenSource cts)
    {
        var msgParser = new ClientMsgParser();

        while (!cts.IsCancellationRequested)
        {
            string? input = await Task.Run(() => Console.ReadLine(), cts.Token);
            
            if (input == null)
            {
                await Disconnect(stream, cts);  // Handle null input (EOF or Ctrl+D)
                Environment.Exit(0);
            }
            
            await ProcessSingleOutgoingMessageAsync(stream, cts, msgParser, input);
        }
    }


    private async Task ProcessSingleOutgoingMessageAsync(
        NetworkStream stream, 
        CancellationTokenSource cts, 
        ClientMsgParser clientMsgParser,
        string input)
    {
        // Parse client's message and send it if it's not a help or rename command
        var msgType = clientMsgParser.GetMsgType(input);
        
        if (_state.IsMessageTypeAllowed(msgType))
        {
            var formattedMessage = clientMsgParser.ParseMsg(input);
            
            UpdateDisplayNameIfNeeded(clientMsgParser, msgType);
            
            _state.ProcessEvent(msgType);

            if (_state.CurrentState == StateType.End)
            {
                await SendByeMessage(stream, cts);
                await Disconnect(stream, cts);
            }

            await SendMessage(stream, cts, formattedMessage);
        }
    }
    private void UpdateDisplayNameIfNeeded(ClientMsgParser clientMsgParser, MessageType msgType)
    {
        if (msgType == MessageType.Rename || msgType == MessageType.Auth)
            _displayName = clientMsgParser.GetDisplayName() ?? "Unknown";
    }

    private static async Task SendMessage(NetworkStream stream, CancellationTokenSource cts, string formattedMessage)
    {
        byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
        await stream.WriteAsync(data, 0, data.Length, cts.Token);
    }
    
    private async Task ProcessIncomingMessagesAsync(NetworkStream stream, CancellationTokenSource cts)
    {
        var msgParser = new ServerMsgParser();
        var buffer = new byte[BufferSize];

        while (!cts.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            
            if (bytesRead > 0)
            {
                await ProcessSingleReceivedMessageAsync(stream, cts, buffer, bytesRead, msgParser);
            }
        }
    }

    private async Task ProcessSingleReceivedMessageAsync(
        NetworkStream stream, 
        CancellationTokenSource cts, 
        byte[] buffer,
        int bytesRead, 
        ServerMsgParser serverMsgParser)
    {
        // Parse server message and print it if it's valid IPK-25-CHAT message
        var msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        var msgType = serverMsgParser.GetMsgType(msg);
                
        if (_state.IsMessageTypeAllowed(msgType))
        {
            _state.ProcessEvent(msgType);
            
            var formattedMsg = serverMsgParser.ParseMsg(msg);
            if (formattedMsg != "" && formattedMsg != "ERROR")
                Console.Write(formattedMsg);
            
            if (formattedMsg == "ERROR")
                await Disconnect(stream, cts);

            if (_state.CurrentState == StateType.End)
            {
                await SendByeMessage(stream, cts);
                await Disconnect(stream, cts);
            }
        }
        if (msgType == MessageType.Unknown)
        {
            Console.Write($"ERROR: {msg}");
            await Disconnect(stream, cts);
        }
    }

    private async Task Disconnect(NetworkStream stream, CancellationTokenSource cts)
    {
        try
        {
            cts.Cancel();
            await stream.FlushAsync();
            Console.WriteLine("Disconnected successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during disconnection: {ex.Message}");
        }
        finally
        {
            stream.Close();
            cts.Dispose();
        }
    }
    private async Task SendByeMessage(NetworkStream stream, CancellationTokenSource cts)
    {
        byte[] data = Encoding.UTF8.GetBytes($"BYE FROM {_displayName}\r\n");
        await stream.WriteAsync(data, 0, data.Length, cts.Token);
    }
}
