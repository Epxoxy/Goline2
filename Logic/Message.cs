using System;
using System.Runtime.Serialization;

namespace Logic
{
    public class Message
    {
        public string Token { get; private set; }
        public object Content { get; private set; }
        public MessageType Type { get; private set; }
        
        public static Message CreateHeart(string token)
        {
            return new Message()
            {
                Token = token,
                Type = MessageType.Heartbeat
            };
        }

        public static Message CreateMessage<T>(string token, T content) where T : ISerializable
        {
            return new Message()
            {
                Token = token,
                Type = MessageType.Message,
                Content = content
            };
        }

        public static Message CreateMessage<T>(string token, T content, MessageType type) where T : ISerializable
        {
            return new Message()
            {
                Token = token,
                Content = content,
                Type = type
            };
        }

        public override string ToString()
        {
            return String.Format("{0}[{1}]：\r\n{2}\r\n", Token, Type, Content);
        }
    }

    public enum MessageType
    {
        Heartbeat,
        Signin,
        Signout,
        Message
    }
}
