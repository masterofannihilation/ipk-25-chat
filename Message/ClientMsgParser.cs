using System.Text.RegularExpressions;
using ipk_25_chat.Message.Enum;
using ipk_25_chat.Message.Interface;

namespace ipk_25_chat.Message;

public class ClientMsgParser : IMsgParser
{
    private string? _displayName;
    private readonly MsgValidator _validator = new();
    
    public string? GetDisplayName()
    {
        return _displayName;
    }
    public string ParseMsg(string msg)
    {
        var msgType = GetMsgType(msg);
        
        var result = msgType switch
        {
            MessageType.Auth => GetAuthMessage(msg),
            MessageType.Join => GetJoinMessage(msg),
            MessageType.Msg => GetNormalMessage(msg),
            MessageType.Help => ShowHelp(),
            MessageType.Rename => ChangeDisplayName(msg),
            _ => throw new ArgumentException($"Unknown message type: {msgType}")
        };
        
        if (result != string.Empty && !_validator.ValidateFormat(msgType, result))
        {
            Console.Write($"ERROR: {result}");
            result = string.Empty;
        }
        
        // Check message length and trim if necessary
        result = _validator.CheckMessageLength(result);
        
        return result;
    }

    public MessageType GetMsgType(string msg)
    {
        if (msg.StartsWith('/'))
        {
            string command = msg.Split(" ")[0];
            
            return command switch
            {
                "/auth" => MessageType.Auth,
                "/join" => MessageType.Join,
                "/rename" => MessageType.Rename,
                "/help" => MessageType.Help,
                _ => MessageType.Unknown
            };
        }
        return MessageType.Msg;
        
    }
    
    private string GetAuthMessage(string msg)
    {
        string?[] msgParts = msg.Split(" ");
        
        var id = msgParts[1];
        var secret = msgParts[2];
        _displayName = msgParts[3];
        
        return $"AUTH {id} AS {_displayName} USING {secret}\r\n";
    }
    
    private string GetJoinMessage(string msg)
    {
        var msgParts = msg.Split(" ");
        
        var channelId = msgParts[1];
        
        return $"JOIN {channelId} AS {_displayName}\r\n";
    }
    
    private string GetNormalMessage(string msg)
    {
        return $"MSG FROM {_displayName} IS {msg}\r\n";
    }
    
    private string ChangeDisplayName(string msg)
    {
        string?[] msgParts = msg.Split(" ");
        
        if (msgParts.Length != 2)
            throw new ArgumentException($"Invalid 'RENAME' message: {msg}");
        
        _displayName = msgParts[1];
        
        return string.Empty;
    }
    private string ShowHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("/auth <id> <secret> <displayName> - Authenticate with the server");
        Console.WriteLine("/join <channelId> - Join a channel");
        Console.WriteLine("/rename <newDisplayName> - Change your display name");
        Console.WriteLine("/help - Show this help message");

        return string.Empty;
    }
}