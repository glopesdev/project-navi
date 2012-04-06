using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectNavi.Hardware
{
    public interface ICommunicationManager
    {
        event EventHandler<CommandCompletedEventArgs> CommandCompleted;

        void CommandAsync(byte[] command, int responseSize, object userState);

        void CancelAsync(byte code);

    }
    public interface IUbiCommunicationManager
    {
        event EventHandler<UbisenseCommandCompletedEventArgs> CommandCompleted;

        void CommandAsync(byte[] command, int responseSize, object userState);

        void CancelAsync(byte code);

    }
    public class UbisensePendingCommand
    { 
        private int tagId;
        private byte[] response;
        private object userState;
        private int offset;
        public UbisensePendingCommand(int tagId, byte[] response, object userState)
        {
            this.tagId = tagId;
            this.response = response;
            this.userState = userState;
            this.offset = 0;
        }

        public int TagId
        {
            get { return tagId; }
        }

        public byte[] Response
        {
            get { return response; }
        }

        public int Offset { get { return offset; } set { offset = value; } }

        public object UserState 
        {
            get { return userState; }
        }
    }

    public class PendingCommand
    {
        private byte code;
        private byte[] response;
        private object userState;
        private int offset;
        public PendingCommand(byte code, byte[] response, object userState)
        {
            this.code = code;
            this.response = response;
            this.userState = userState;
            this.offset = 0;
        }

        public byte Code
        {
            get { return code; }
        }

        public byte[] Response
        {
            get { return response; }
        }

        public int Offset { get { return offset; } set { offset = value; } }

        public object UserState 
        {
            get { return userState; }
        }
    }
}
