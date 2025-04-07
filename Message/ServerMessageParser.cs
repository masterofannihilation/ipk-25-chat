using System;
using System.Net;
using System.Text.RegularExpressions;

namespace ipk_25_chat.Message;

public class ServerMessageParser : IMsgParser
{

    public string ParseMsg(string msg)
    {
        
        var msgType = GetMsgType(msg);

        return msgType switch
        {
            IMsgParser.MessageType.Msg => ParseNormalMessage(msg),
            IMsgParser.MessageType.Err => ParseErrorMessage(msg),
            IMsgParser.MessageType.Bye => ParseByeMessage(msg),
            IMsgParser.MessageType.Reply => ParseReplyMessage(msg),
            _ => ""
        };
    }

    public IMsgParser.MessageType GetMsgType(string msg)
    {
        string type = msg.Split(" ")[0];

        return type switch
        {
            "MSG" => IMsgParser.MessageType.Msg,
            "ERR" => IMsgParser.MessageType.Err,
            "BYE" => IMsgParser.MessageType.Bye,
            "REPLY" => IMsgParser.MessageType.Reply,
            _ => IMsgParser.MessageType.Unknown
        };
    }

    private string ParseNormalMessage(string msg)
    {
        var msgParts = msg.Split(" ", 4);
        if (msgParts.Length != 4)
            throw new ArgumentException($"Invalid 'MSG' message: {msg}");

        var displayName = msgParts[2];
        var content = msgParts[3];

        return $"{displayName}: {content}";
    }

    private string ParseErrorMessage(string msg)
    {
        var msgParts = msg.Split(" ", 4);
        if (msgParts.Length != 4)
            throw new ArgumentException($"Invalid 'ERR' message: {msg}");

        var displayName = msgParts[2];
        var content = msgParts[3];

        return $"Error from {displayName}: {content}";
    }

    private string ParseByeMessage(string msg)
    {
        var msgParts = msg.Split(" ");
        if (msgParts.Length != 3)
            throw new ArgumentException($"Invalid 'BYE' message: {msg}");

        var displayName = msgParts[2];

        return $"Goodbye from {displayName}";
    }

    private string ParseReplyMessage(string msg)
    {
        var msgParts = msg.Split(" ", 4);
        if (msgParts.Length != 4)
            throw new ArgumentException($"Invalid 'REPLY' message: {msg}");

        var status = msgParts[1];
        var content = msgParts[3];

        return $"Action {status}: {content}";
    }
}