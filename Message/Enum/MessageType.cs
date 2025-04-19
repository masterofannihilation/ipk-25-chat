namespace ipk_25_chat.Message.Enum;

// Types that have value starting with 0xA are placeholder values
public enum MessageType
{
    Auth = 0x02,
    Join = 0x03,
    Msg = 0x04,
    Rename = 0xA1,
    Help = 0xA2,
    Err = 0xFE,
    Reply = 0x01,
    NotReply = 0xA3,
    Bye = 0xFF,
    Confirm = 0x00,
    Ping = 0xFD,
    Unknown = 0xA4
}