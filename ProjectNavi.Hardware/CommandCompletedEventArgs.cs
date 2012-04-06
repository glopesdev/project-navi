using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;

namespace ProjectNavi.Hardware
{
    public class CommandCompletedEventArgs : AsyncCompletedEventArgs
    {
        public CommandCompletedEventArgs(byte command, byte[] response, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            Response = response;
            Command = command;
        }

        public byte[] Response { get; private set; }

        public byte Command { get; private set; }
    }

    public class UbisenseCommandCompletedEventArgs : AsyncCompletedEventArgs
    {
        public UbisenseCommandCompletedEventArgs(int tagId, byte[] response, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            Response = response;
            TagId = tagId;
        }

        public byte[] Response { get; private set; }

        public int TagId { get; private set; }
    }
}
