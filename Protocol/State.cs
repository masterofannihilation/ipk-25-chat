using System;
using ipk_25_chat.Message;

namespace ipk_25_chat.Protocol;

public enum StateType
{
    Start,
    Auth,
    Open,
    Join,
    End,
}

public class State
{
    public StateType CurrentState { get; private set; } = StateType.Start;

    public void HandleEvent(MessageType msgType)
    {
        switch (CurrentState)
        {
           case StateType.Start:
               if (msgType == MessageType.Auth)
               {
                   CurrentState = StateType.Auth;
               }
               else if (msgType == MessageType.Bye || msgType == MessageType.Err)
               {
                   CurrentState = StateType.End;
               }

               break;
           case StateType.Auth:
               if (msgType == MessageType.Reply)
               {
                    CurrentState = StateType.Open;
               }
               else if (msgType == MessageType.Bye || msgType == MessageType.Err)
               {
                   CurrentState = StateType.End;
               }

               break;
           case StateType.Open:
               if (msgType == MessageType.Join)
               {
                   CurrentState = StateType.Join;
               }
               else if (msgType == MessageType.Msg)
               {
                   CurrentState = CurrentState;
               }
               else if (msgType == MessageType.Bye || msgType == MessageType.Err || msgType == MessageType.Reply || msgType == MessageType.NotReply)
               {
                   Console.WriteLine("Here");
                   CurrentState = StateType.End;
               }

               break;
           case StateType.Join:
               if (msgType == MessageType.NotReply || msgType == MessageType.Reply)
               {
                   CurrentState = StateType.Open;
               }
               else if (msgType == MessageType.Bye || msgType == MessageType.Err)
               {
                   CurrentState = StateType.End;
               }

               break;
        }
    }

    public bool IsMessageTypeAllowed(MessageType type)
    {
        switch (CurrentState)
        {
            case StateType.Start:
                return type == MessageType.Auth || type == MessageType.Bye || type == MessageType.Err;
            case StateType.Auth:
                return type == MessageType.NotReply || type == MessageType.Reply || type == MessageType.Bye || type == MessageType.Err;
            case StateType.Open:
                return type == MessageType.Msg || type == MessageType.Join || type == MessageType.Bye || type == MessageType.Err || type == MessageType.Reply || type == MessageType.NotReply;
            case StateType.Join:
                return type == MessageType.Msg || type == MessageType.NotReply || type == MessageType.Reply || type == MessageType.Bye || type == MessageType.Err;
            default:
                return false;
        }
    }
}