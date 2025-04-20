using System.Text.RegularExpressions;
using ipk_25_chat.Message.Enum;

namespace ipk_25_chat.Message;

public class MsgValidator
{
    public bool ValidateFormat(MessageType type, string msg)
    {
        if (!msg.EndsWith("\r\n", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"ERROR: {msg}");
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
        var msgParts = msg.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        var content = GetContent(msg, "IS");
        return msgParts.Length >= 4 &&
               msgParts[0].Equals("REPLY", StringComparison.OrdinalIgnoreCase) &&
               (msgParts[1].Equals("OK", StringComparison.OrdinalIgnoreCase) || msgParts[1].Equals("NOK", StringComparison.OrdinalIgnoreCase)) &&
               msgParts[2].Equals("IS", StringComparison.OrdinalIgnoreCase) &&
               IsValidContent(content);
    }

    private bool IsValidByeMsg(string msg)
    {
        var msgParts = msg.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        return msgParts.Length == 3 &&
               msgParts[0].Equals("BYE", StringComparison.OrdinalIgnoreCase) &&
               msgParts[1].Equals("FROM", StringComparison.OrdinalIgnoreCase) &&
               IsValidDisplayName(msgParts[2]);
    }

    private bool IsValidAuthMsg(string msg)
    {
        var msgParts = msg.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        return msgParts.Length == 6 &&
               msgParts[0].Equals("AUTH", StringComparison.OrdinalIgnoreCase) &&
               msgParts[2].Equals("AS", StringComparison.OrdinalIgnoreCase) &&
               msgParts[4].Equals("USING", StringComparison.OrdinalIgnoreCase) &&
               IsValidId(msgParts[1]) &&
               IsValidDisplayName(msgParts[3]) &&
               IsValidSecret(msgParts[5]);
    }

    private bool IsValidJoinMsg(string msg)
    {
        var msgParts = msg.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        return msgParts.Length == 4 &&
               msgParts[0].Equals("JOIN", StringComparison.OrdinalIgnoreCase) &&
               msgParts[2].Equals("AS", StringComparison.OrdinalIgnoreCase) &&
               IsValidId(msgParts[1]) &&
               IsValidDisplayName(msgParts[3]);
    }

    private bool IsValidNormalMsg(string msg)
    {
        var msgParts = msg.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        var content = GetContent(msg, "IS");
        return msgParts.Length >= 4 &&
               msgParts[0].Equals("MSG", StringComparison.OrdinalIgnoreCase) &&
               msgParts[1].Equals("FROM", StringComparison.OrdinalIgnoreCase) &&
               msgParts[3].Equals("IS", StringComparison.OrdinalIgnoreCase) &&
               IsValidDisplayName(msgParts[2]) &&
               IsValidContent(content);
    }

    public string CheckMessageLength(string msg)
    {
        var content = GetContent(msg, "IS");
        if (content.Length > 60000)
        {
            Console.WriteLine("ERROR: Message is too long, max 60000 characters");
            int index = msg.IndexOf("IS", StringComparison.OrdinalIgnoreCase);
            var msgBase = msg.Substring(0,index + 3);
            var contentTrimmed = content.Substring(0, 60000);
            return msgBase + contentTrimmed + "\r\n";
        }
        return msg;
    }

    public string GetContent(string msg, string delimiter)
    {
        int index = msg.IndexOf(delimiter, StringComparison.OrdinalIgnoreCase);
        if (index == -1) return string.Empty;
        var content = msg.Substring(index + delimiter.Length + 1);
        return content;
    }

    private bool IsValidErrMsg(string msg)
    {
        var msgParts = msg.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        var content = GetContent(msg, "IS");
        return msgParts.Length >= 4 &&
               msgParts[0].Equals("ERR", StringComparison.OrdinalIgnoreCase) &&
               msgParts[1].Equals("FROM", StringComparison.OrdinalIgnoreCase) &&
               msgParts[3].Equals("IS", StringComparison.OrdinalIgnoreCase) &&
               IsValidDisplayName(msgParts[2]) &&
               IsValidContent(content);
    }

    private bool IsValidId(string id)
    {
        var regex = new Regex(@"^[a-zA-Z0-9_-]{1,20}$");
        return regex.IsMatch(id);
    }

    private bool IsValidSecret(string secret)
    {
        var regex = new Regex(@"^[a-zA-Z0-9_-]{1,128}$");
        return regex.IsMatch(secret.Trim());
    }

    private bool IsValidDisplayName(string displayName)
    {
        var regex = new Regex(@"^\S{1,20}$");
        return regex.IsMatch(displayName.Trim());
    }

    private bool IsValidContent(string message)
    {
        var regex = new Regex(@"^[\x21-\x7E\x20\x0A]*$");
        return regex.IsMatch(message.Trim());
    }
}