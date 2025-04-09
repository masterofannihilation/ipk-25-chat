using System.Text.RegularExpressions;
using ipk_25_chat.Message.Interface;

namespace ipk_25_chat.Message;

public class ServerMsgParser : IMsgParser
{
    private readonly MsgValidator _validator = new();
    
    public string ParseMsg(string msg)
    {
        var msgType = GetMsgType(msg);
        if (!_validator.ValidateFormat(msgType, msg))
        {
            Console.Write($"ERROR: {msg}");
            return "ERROR";
        }

        return msgType switch
        {
            MessageType.Msg => ParseNormalMessage(msg),
            MessageType.Err => ParseErrorMessage(msg),
            MessageType.Reply => ParseReplyMessage(msg),
            MessageType.NotReply => ParseNotReplyMessage(msg),
            _ => ""
        };
    }

    public MessageType GetMsgType(string msg)
    {
        var msgParts = msg.Split(" ");
        if (msgParts.Length < 2)
            return MessageType.Unknown;
        
        return msgParts[0] switch
        {
            "MSG" => MessageType.Msg,
            "ERR" => MessageType.Err,
            "BYE" => MessageType.Bye,
            "REPLY" => msgParts[1] switch
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
        var displayName = msgParts[2];
        var content = _validator.GetContent(msg, "IS");

        return $"{displayName}: {content}\n";
    }

    private string ParseErrorMessage(string msg)
    {
        string?[] msgParts = msg.Split(" ");
        var displayName = msgParts[2];
        var content = _validator.GetContent(msg, "IS");

        return $"ERROR FROM {displayName}: {content}\n";
    }

    private string ParseReplyMessage(string msg)
    {
        var content = _validator.GetContent(msg, "IS");

        return $"Action Success: {content}";
    }
    
    
    private string ParseNotReplyMessage(string msg)
    {
        var content = _validator.GetContent(msg, "IS");
        
        return $"Action Failure: {content}";
    }
}