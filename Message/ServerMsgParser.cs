using System.Text.RegularExpressions;
using ipk_25_chat.Message.Interface;

namespace ipk_25_chat.Message;

public class ServerMsgParser : IMsgParser
{
    private string? _displayName;
    private readonly MsgValidator _validator = new();
    public string ParseMsg(string msg)
    {
        var msgType = GetMsgType(msg);
        if (!_validator.ValidateFormat(msgType, msg))
        {
            Console.Error.WriteLine($"Invalid message format from server: {Regex.Escape(msg)}");
            throw new ArgumentException($"Invalid message format: {msg}");
        }

        return msgType switch
        {
            MessageType.Msg => ParseNormalMessage(msg),
            MessageType.Err => ParseErrorMessage(msg),
            MessageType.Bye => ParseByeMessage(),
            MessageType.Reply => ParseReplyMessage(msg),
            MessageType.NotReply => ParseNotReplyMessage(msg),
            _ => ""
        };
    }

    public MessageType GetMsgType(string msg)
    {
        string type = msg.Split(" ")[0];
        string response = msg.Split(" ")[1];

        return type switch
        {
            "MSG" => MessageType.Msg,
            "ERR" => MessageType.Err,
            "BYE" => MessageType.Bye,
            "REPLY" => response switch
            {
                "OK" => MessageType.Reply,
                "NOK" => MessageType.NotReply,
                _ => MessageType.Unknown
            },
            "JOIN" => MessageType.Join,
            _ => MessageType.Unknown
        };
    }

    private string ParseNormalMessage(string msg)
    {
        string?[] msgParts = msg.Split(" ");
        _displayName = msgParts[2];
        var content = _validator.GetContent(msg, "IS");

        return $"{_displayName}: {content}\n";
    }

    private string ParseErrorMessage(string msg)
    {
        string?[] msgParts = msg.Split(" ");
        var displayName = msgParts[2];
        var content = _validator.GetContent(msg, "IS");

        return $"ERROR FROM {displayName}: {content}\n";
    }

    private string ParseByeMessage()
    {
        return "BYE";
    }

    private string ParseReplyMessage(string msg)
    {
        var content = _validator.GetContent(msg, "IS");

        return $"Action Success: {content}";
    }
    
    private string ParseNotReplyMessage(string msg)
    {
        var content = _validator.GetContent(msg, "IS");
        
        return $"Action Success: {content}";
    }
}