using System;
using ipk_25_chat.Message;
using ipk_25_chat.Message.Enum;

namespace ipk_25_chat.Protocol;
public class State
{
    public StateType CurrentState { get; private set; } = StateType.Start;

    public void ProcessEvent(MessageType msgType)
    {
        switch (CurrentState)
        {
           case StateType.Start:
               CurrentState = msgType switch
               {
                   MessageType.Auth => StateType.Auth,
                   MessageType.Bye or MessageType.Err => StateType.End,
                   _ => CurrentState
               };

               break;
           case StateType.Auth:
               CurrentState = msgType switch
               {
                   MessageType.Reply => StateType.Open,
                   MessageType.Bye or MessageType.Err => StateType.End,
                   _ => CurrentState
               };

               break;
           case StateType.Open:
               CurrentState = msgType switch
               {
                   MessageType.Join => StateType.Join,
                   MessageType.Msg => CurrentState,
                   MessageType.Bye or MessageType.Err or MessageType.Reply or MessageType.NotReply => StateType.End,
                   _ => CurrentState
               };

               break;
           case StateType.Join:
               CurrentState = msgType switch
               {
                   MessageType.NotReply or MessageType.Reply => StateType.Open,
                   MessageType.Bye or MessageType.Err => StateType.End,
                   _ => CurrentState
               };

               break;
        }
    }

    public bool IsMessageTypeAllowed(MessageType type)
    {
        // Client can use /help command at any time
        if (type == MessageType.Help)
            return true;
        
        return CurrentState switch
        {
            StateType.Start => type is MessageType.Auth or MessageType.Bye or MessageType.Err,
            StateType.Auth => type is MessageType.NotReply or MessageType.Auth or MessageType.Reply or MessageType.Bye or MessageType.Err,
            StateType.Open => type is MessageType.Msg or MessageType.Join or MessageType.Bye or MessageType.Err
                or MessageType.Reply or MessageType.NotReply or MessageType.Rename,
            StateType.Join => type is MessageType.Msg or MessageType.NotReply or MessageType.Reply or MessageType.Bye
                or MessageType.Err,
            _ => false
        };
    }
}