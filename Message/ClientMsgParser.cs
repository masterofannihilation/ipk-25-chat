using System;
using System.Text.RegularExpressions;

namespace ipk_25_chat.Message;

public class ClientMsgParser : IMsgParser
{
    private string _displayName;

    public string ParseMsg(string msg)
    {
        var msgType = GetMsgType(msg);
        
        return msgType switch
        {
            IMsgParser.MessageType.Auth => GetAuthMessage(msg),
            IMsgParser.MessageType.Join => GetJoinMessage(msg),
            IMsgParser.MessageType.Msg => GetNormalMessage(msg),
            IMsgParser.MessageType.Help => ShowHelp(),
            IMsgParser.MessageType.Rename => ChangeDisplayName(msg),
            _ => throw new ArgumentException($"Unknown message type: {msgType}")
        };
    }

    public IMsgParser.MessageType GetMsgType(string msg)
    {
        if (msg.StartsWith('/'))
        {
            string command = msg.Split(" ")[0];
            
            return command switch
            {
                "/auth" => IMsgParser.MessageType.Auth,
                "/join" => IMsgParser.MessageType.Join,
                "/rename" => IMsgParser.MessageType.Rename,
                "/help" => IMsgParser.MessageType.Help,
                _ => IMsgParser.MessageType.Help
            };
        }
        return IMsgParser.MessageType.Msg;
        
    }
    
    private string GetAuthMessage(string msg)
    {
        var msgParts = msg.Split(" ");

        if (msgParts.Length != 4)
            throw new ArgumentException($"Invalid 'AUTH' message: {msg}");
        
        var id = msgParts[1];
        var secret = msgParts[2];
        _displayName = msgParts[3];
        
        if (!ValidId(id))
            throw new ArgumentException($"Invalid format of ID: {id}");
        
        if (!ValidSecret(secret))
            throw new ArgumentException($"Invalid format of secret: {secret}");
        
        if (!ValidDisplayName(_displayName))
            throw new ArgumentException($"Invalid format of display name: {_displayName}");
        
        return $"AUTH {id} AS {_displayName} USING {secret}\r\n";
    }
    
    private string GetJoinMessage(string msg)
    {
        var msgParts = msg.Split(" ");

        if (msgParts.Length != 2)
            throw new ArgumentException($"Invalid 'JOIN' message: {msg}");
        
        var channelId = msgParts[1];
        if (!ValidId(channelId))
            throw new ArgumentException($"Invalid format of channel ID: {channelId}");
        
        return $"JOIN {channelId} AS {_displayName}\r\n";
    }
    
    private string GetNormalMessage(string msg)
    {
        if (!ValidMessageContent(msg))
            throw new ArgumentException($"Invalid format of message: {msg}");
        
        return $"MSG FROM {_displayName} IS {msg}\r\n";
    }
    
    private string ChangeDisplayName(string msg)
    {
        var msgParts = msg.Split(" ");
        
        if (msgParts.Length != 2)
            throw new ArgumentException($"Invalid 'RENAME' message: {msg}");
        
        _displayName = msgParts[1];
        
        
        return $"NEW: ${_displayName}";
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

    private bool ValidId(string id)
    {
        var regex = new Regex(@"^[a-zA-Z0-9_-]{1,20}$");
        return regex.IsMatch(id);
    }
    
    private bool ValidSecret(string secret)
    {
        var regex = new Regex(@"^[a-zA-Z0-9_-]{1,128}$");
        return regex.IsMatch(secret);
    }

    private bool ValidDisplayName(string displayName)
    {
        var regex = new Regex(@"^\S{1,20}$");
        return regex.IsMatch(displayName);
    }

    private bool ValidMessageContent(string message)
    {
        var regex = new Regex(@"^[\x21-\x7E\x20\x0A]{1,60000}$");
        return regex.IsMatch(message);
    }

}