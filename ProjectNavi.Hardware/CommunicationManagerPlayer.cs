using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace ProjectNavi.Hardware
{
    public class CommunicationManagerPlayer : HardwareComponent, ICommunicationManager
    {
        bool disposed;
        StreamReader reader;
        Timer responseTimer;
        CommandCompletedEventArgs nextResponse;

        public CommunicationManagerPlayer(string path)
        {
            reader = new StreamReader(path);
            responseTimer = new Timer(ResponseCallback);
        }

        public event EventHandler<CommandCompletedEventArgs> CommandCompleted;

        private void OnCommandCompleted(CommandCompletedEventArgs e)
        {
            var handler = CommandCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void ParseNextResponse(object userState)
        {
            var line = reader.ReadLine();
            var arguments = line.Split(',');

            var offset = DateTimeOffset.Parse(arguments[0]);
            nextResponse = new CommandCompletedEventArgs(Convert.ToByte(arguments[1]), Convert.FromBase64String(arguments[2]), null, false, userState);
            responseTimer.Change(0, (int)offset.Offset.TotalMilliseconds);
        }

        public void CommandAsync(byte[] command, int responseSize, object userState)
        {
            ParseNextResponse(userState);
        }

        public void CancelAsync(byte code)
        {
        }

        private void ResponseCallback(object state)
        {
            responseTimer.Change(Timeout.Infinite, Timeout.Infinite);
            OnCommandCompleted(nextResponse);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    reader.Close();
                    disposed = true;
                }
            }

            base.Dispose(disposing);
        }
    }
}
