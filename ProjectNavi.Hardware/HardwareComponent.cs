using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectNavi.Hardware
{
    public class HardwareComponent : IDisposable
    {
        bool disposed;

        public event EventHandler Disposed;

        ~HardwareComponent()
        {
            Dispose(false);
        }

        protected virtual void OnDisposed(EventArgs e)
        {
            var handler = Disposed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    OnDisposed(EventArgs.Empty);
                    disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
