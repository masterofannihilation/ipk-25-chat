namespace ipk_25_chat.Message;

public interface IMsgParser 
{
    public enum MessageType {
        Auth,
        Join,
        Msg,
        Rename,
        Help,
        Err,
        Reply,
        Bye,
        Confirm,
        Ping,
        Unknown
    }
    public string ParseMsg(string msg);
    
    public MessageType GetMsgType(string msg);
    
}