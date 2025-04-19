using ipk_25_chat.Message.Enum;

namespace ipk_25_chat.Message.Interface;

public interface IMsgParser 
{
    public string ParseMsg(string msg);
    public MessageType GetMsgType(string msg);
}