using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Disposables;
using System.IO;

namespace ProjectNavi.Hardware
{
    public class CommunicationManagerLogger : HardwareComponent
    {
        bool disposed;
        StreamWriter writer;
        IDisposable commandCompleted;

        public CommunicationManagerLogger(ICommunicationManager manager, string path)
        {
            writer = new StreamWriter(path);
            manager.CommandCompleted += manager_CommandCompleted;
            commandCompleted = Disposable.Create(() => manager.CommandCompleted -= manager_CommandCompleted);
        }

        void manager_CommandCompleted(object sender, CommandCompletedEventArgs e)
        {
            var line = string.Format("{0},{1},{2}", DateTimeOffset.Now, e.Command, Convert.ToBase64String(e.Response));
            writer.WriteLine(line);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    commandCompleted.Dispose();
                    writer.Close();
                    disposed = true;
                }
            }

            base.Dispose(disposing);
        }
    }
}
