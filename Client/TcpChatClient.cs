using System;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ipk_25_chat.Message;
using ipk_25_chat.Message.Enum;
using ipk_25_chat.Protocol;

namespace ipk_25_chat.Client;

public class TcpChatClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly State _state = new();
    private string _displayName = "Unknown";
    private const int BufferSize = 1024;
    private bool _isDisconnected;
    
    private readonly Channel<string> _userInput = Channel.CreateUnbounded<string>();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private TaskCompletionSource<bool> _responseTcs = new ();

    public TcpChatClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task RunAsync()
    {
        // Establish TCP connection
        using var client = new TcpClient();
        await client.ConnectAsync(_host, _port);
        
        NetworkStream stream = client.GetStream();
        
        RegisterCancelKeyPress(stream, _cts);
        
        var readUserInputTask = ReadUserInput(_cts);
        var sendTask = ProcessUserInputAsync(stream, _cts);
        var receiveTask = ProcessServerInputAsync(stream, _cts);

        await Task.WhenAll(readUserInputTask,sendTask, receiveTask);
        _cts.Dispose();
    }
    
    private void RegisterCancelKeyPress(NetworkStream stream, CancellationTokenSource cts)
    {
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            await SendByeMessage(stream, cts);
            await Disconnect(stream, cts,0);
            Environment.Exit(0);
        };
    }

    private Task ReadUserInput(CancellationTokenSource cts)
    {
        // Start a task to read user input and write it to the channel
        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                string? input = Console.ReadLine();
                // Handle EOF or Ctrl+D, which indicates no more input, signal it to the channel
                if (input == null)
                {
                    _userInput.Writer.Complete();
                    break;
                }
                await _userInput.Writer.WriteAsync(input, cts.Token);
            }
        }, cts.Token);
        return Task.CompletedTask;
    }

    private async Task ProcessUserInputAsync(NetworkStream stream, CancellationTokenSource cts)
    {
        var msgParser = new ClientMsgParser();
        // Process messages from the channel
        try
        {
            await foreach (var input in _userInput.Reader.ReadAllAsync(cts.Token))
            {
                await ProcessOutgoingMessageAsync(stream, cts, msgParser, input);
            }
        }
        catch (OperationCanceledException)
        {
            await Disconnect(stream, cts,0);
            Environment.Exit(0);
        }
        // Terminate the application after processing all messages
        finally
        {
            await SendByeMessage(stream, cts);
            await Disconnect(stream, cts,0);
        }
    }

    private async Task ProcessOutgoingMessageAsync(NetworkStream stream, CancellationTokenSource cts, ClientMsgParser clientMsgParser, string input)
    {
        await _semaphore.WaitAsync();
        try
        {
            var msgType = clientMsgParser.GetMsgType(input);
            // Check if the message type the client is trying to send is allowed in the current state
            if (_state.IsMessageTypeAllowed(msgType))
            {
                var formattedMessage = clientMsgParser.ParseMsg(input);
                // Check if the user set its display name in an auth message or changed it using /rename
                UpdateDisplayNameIfNeeded(clientMsgParser, msgType);
                // If the client entered the END state, send a BYE message to the server and disconnect
                await TerminateConnectionIfEndState(stream, cts);
                // Send a formatted message to the server
                if (formattedMessage != "")
                {
                    // Change the state of the client based on the message type
                    _state.ProcessEvent(msgType);
                    await SendMessageAsync(stream, cts, formattedMessage);
                    // Wait for response only when authorizing the user or joining a channel
                    await WaitForResponseFromServerAsync(stream, cts);
                }
            }
            else
            {
                Console.WriteLine( $"ERROR: This message type '{msgType}' is not allowed in the current state '{_state.CurrentState}'");
                ShowHelp();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private void UpdateDisplayNameIfNeeded(ClientMsgParser clientMsgParser, MessageType msgType)
    {
        if (msgType is MessageType.Rename or MessageType.Auth)
            _displayName = clientMsgParser.GetDisplayName() ?? "Unknown";
    }
    private async Task TerminateConnectionIfEndState(NetworkStream stream, CancellationTokenSource cts)
    {
        if (_state.CurrentState == StateType.End)
        {
            await SendByeMessage(stream, cts);
            await Disconnect(stream, cts,0);
        }
    }
    private async Task SendByeMessage(NetworkStream stream, CancellationTokenSource cts)
    {
        byte[] data = Encoding.UTF8.GetBytes($"BYE FROM {_displayName}\r\n");
        await stream.WriteAsync(data, 0, data.Length, cts.Token);
    }
    private static async Task SendMessageAsync(NetworkStream stream, CancellationTokenSource cts, string formattedMessage)
    {
        // Do not try to send a message if cancellation is requested
        if (cts.Token.IsCancellationRequested)
            return; 

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
            await stream.WriteAsync(data, 0, data.Length, cts.Token);
        }
        catch (ObjectDisposedException)
        {
            await Console.Error.WriteLineAsync("ERROR: Attempted to write to a disposed stream.");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Failed to send message: {ex.Message}");
            Environment.Exit(1);
        }
    }
    
    private async Task WaitForResponseFromServerAsync(NetworkStream stream, CancellationTokenSource cts)
    {
        if (_state.CurrentState is StateType.Auth or StateType.Join)
        {
            var responseTask = _responseTcs.Task;
            // Wait for the server response or timeout (5 seconds)
            if (await Task.WhenAny(responseTask, Task.Delay(5000)) != responseTask)
            {
                Console.WriteLine("ERROR: Server response timeout");
                await SendErrMessage(stream, cts);
                await Disconnect(stream, cts,1 );
                Environment.Exit(1);
            }
            // Reset the TaskCompletionSource for the next message
            _responseTcs = new();
        }
    }
    
    private async Task SendErrMessage(NetworkStream stream, CancellationTokenSource cts, string invalidMsg="")
    {
        invalidMsg = invalidMsg.Trim();
        byte[] data = Encoding.UTF8.GetBytes($"ERR FROM {_displayName} IS {invalidMsg}\r\n");
        await stream.WriteAsync(data, 0, data.Length, cts.Token);
    }

    private async Task ProcessServerInputAsync(NetworkStream stream, CancellationTokenSource cts)
    {
        var msgParser = new ServerMsgParser();
        var buffer = new byte[BufferSize];
        var dataBuffer = new StringBuilder(); // Buffer to store partial data

        try
        {
            while (!cts.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                if (bytesRead > 0)
                {
                    // Append received data to the buffer
                    dataBuffer.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                    // Process complete messages
                    string data = dataBuffer.ToString();
                    int delimiterIndex;
                    while ((delimiterIndex = data.IndexOf("\r\n", StringComparison.Ordinal)) != -1)
                    {
                        // Extract a complete message, including the delimiter
                        string completeMessage = data.Substring(0, delimiterIndex + 2);
                        data = data.Substring(delimiterIndex + 2); // Remove a processed message

                        // Process the complete message
                        await ProcessReceivingMessageAsync(stream, cts, Encoding.UTF8.GetBytes(completeMessage), completeMessage.Length, msgParser);
                    }

                    // Update the buffer with remaining data
                    dataBuffer.Clear();
                    dataBuffer.Append(data);
                }
            }
        }
        catch (OperationCanceledException)
        {
            await Disconnect(stream, cts, 0);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"ERROR: An unexpected error occurred while reading from the server: {ex.Message}");
            await Disconnect(stream, cts,1);
            Environment.Exit(1);
        }
    }

    private async Task ProcessReceivingMessageAsync(NetworkStream stream, CancellationTokenSource cts, byte[] buffer, int bytesRead, ServerMsgParser serverMsgParser)
    {
        // Parse a server message and print it if it's a valid IPK-25-CHAT message
        var msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        var msgType = serverMsgParser.GetMsgType(msg);
        
        if (msgType == MessageType.Unknown || !_state.IsMessageTypeAllowed(msgType))
        {
            Console.Write($"ERROR: {msg}");
            await SendErrMessage(stream, cts, msg);
            await Disconnect(stream, cts, 1);
        }
        else
        {

            // Format server message for display, check if the format is valid
            var formattedMsg = serverMsgParser.ParseMsg(msg);
            if (formattedMsg != "" && formattedMsg != "ERROR")
                Console.Write(formattedMsg);

            // Malformed message from the server
            if (formattedMsg == "ERROR")
            {
                Console.Write($"ERROR: {msg}");
                await SendErrMessage(stream, cts, msg);
                await Disconnect(stream, cts, 1);
            }

            // Change the state of the client based on the message type
            _state.ProcessEvent(msgType);
            await TerminateConnectionIfEndState(stream, cts);

            // Signal that a response has been received and another user message can be processed
            if (msgType is MessageType.Reply or MessageType.NotReply)
                _responseTcs.TrySetResult(true);
        }
    }

    private async Task Disconnect(NetworkStream stream, CancellationTokenSource cts, int retCode)
    {
        if (_isDisconnected) return; // Prevent multiple disconnections
            _isDisconnected = true;
        
        try
        {
            await cts.CancelAsync();
            await stream.FlushAsync();
            Environment.Exit(retCode);
            // Console.WriteLine("Disconnected successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during disconnection: {ex.Message}");
        }
        finally
        {
            stream.Close();
        }
    }
    
    private void ShowHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("/auth <id> <secret> <displayName> - Authenticate with the server");
        Console.WriteLine("/join <channelId> - Join a channel");
        Console.WriteLine("/rename <newDisplayName> - Change your display name");
        Console.WriteLine("/help - Show this help message");
    }
}
