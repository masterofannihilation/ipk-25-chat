using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using ipk_25_chat.Message.Enum;

namespace ipk_25_chat.Message;

public class MsgValidator
{
    public bool ValidateFormat(MessageType type, string msg)
    {
        if (!msg.EndsWith("\r\n"))
        {
            Console.WriteLine($"Message doesn't end with \\r\\n: {Regex.Escape(msg)}");
            return false;
        }

        return type switch
        {
            MessageType.Auth => IsValidAuthMsg(msg),
            MessageType.Join => IsValidJoinMsg(msg),
            MessageType.Msg => IsValidNormalMsg(msg),
            MessageType.Err => IsValidErrMsg(msg),
            MessageType.Reply => IsValidReplyMsg(msg),
            MessageType.NotReply => IsValidReplyMsg(msg),
            MessageType.Bye => IsValidByeMsg(msg),
            MessageType.Unknown => false,
            _ => false
        };
    }

    private bool IsValidReplyMsg(string msg)
    {
        var msgParts = msg.Split(" ");
        var content = GetContent(msg, "IS");
        return msgParts is ["REPLY", "OK" or "NOK", "IS", ..] &&
               IsValidContent(content);
    }

    private bool IsValidByeMsg(string msg)
    {
        var msgParts = msg.Split(" ");
        return msgParts is ["BYE", "FROM", _] &&
               IsValidDisplayName(msgParts[2]);
    }

    private bool IsValidAuthMsg(string msg)
    {
        var msgParts = msg.Split(" ");
        return msgParts is ["AUTH", _, "AS", _, "USING", _] &&
               IsValidId(msgParts[1]) &&
               IsValidDisplayName(msgParts[3])  &&
               IsValidSecret(msgParts[5]);
    }

    private bool IsValidJoinMsg(string msg)
    {
        var msgParts = msg.Split(" ");
        return msgParts is ["JOIN", _, "AS" , _] &&
               IsValidId(msgParts[1]) &&
               IsValidDisplayName(msgParts[3]);
    }

    private bool IsValidNormalMsg(string msg)
    {
        msg = CheckMessageLength(msg);
        var msgParts = msg.Split(" ");
        var content = GetContent(msg, "IS");
        return msgParts is ["MSG", "FROM", _, "IS", ..] &&
               IsValidDisplayName(msgParts[2]) &&
               IsValidContent(content);
    }

    private static string CheckMessageLength(string msg)
    {
        if (msg.Length > 60000)
        {
            Console.WriteLine("ERROR: Message is too long, max 60000 characters");
            msg = msg.Substring(0, 60000);
            
        }

        return msg;
    }
    public string GetContent(string msg, string delimiter)
    {
        int index = msg.IndexOf(delimiter, StringComparison.Ordinal);
        var content = msg.Substring(index + 3);
        return content;
    }

    private bool IsValidErrMsg(string msg)
    {
        var msgParts = msg.Split(" ");
        var content = GetContent(msg, "IS");
        return msgParts is ["ERR", "FROM", _, "IS", ..] &&
               IsValidDisplayName(msgParts[2]) &&
               IsValidContent(content);
    }
    
    
    public bool IsValidId(string id)
    {
        var regex = new Regex(@"^[a-zA-Z0-9_-]{1,20}$");
        if (!regex.IsMatch(id))
        {
            Console.WriteLine($"Invalid id: {Regex.Escape(id)}");
        }
        return regex.IsMatch(id);
    }
    
    public bool IsValidSecret(string secret)
    {
        var regex = new Regex(@"^[a-zA-Z0-9_-]{1,128}$");
        if (!regex.IsMatch(secret.Trim()))
        {
            Console.WriteLine($"Invalid secret: {Regex.Escape(secret)}");
        }
        return regex.IsMatch(secret.Trim());
    }

    public bool IsValidDisplayName(string displayName)
    {
        var regex = new Regex(@"^\S{1,20}$");
        if (!regex.IsMatch(displayName.Trim()))
        {
            Console.WriteLine($"Invalid display name: {Regex.Escape(displayName)}");
        }
        return regex.IsMatch(displayName.Trim());
    }

    public bool IsValidContent(string message)
    {
        var regex = new Regex(@"^[\x21-\x7E\x20\x0A]*$");
        if (!regex.IsMatch(message.Trim()))
        {
            Console.WriteLine($"Invalid content: {Regex.Escape(message)}");
        }
        return regex.IsMatch(message.Trim());
    }
}
