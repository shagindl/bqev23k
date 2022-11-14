using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQEV23K
{
    public class LogMutex : IDisposable
    {
        private System.Threading.Mutex mutex = new System.Threading.Mutex();
        private Logs.DebugLog log;
        string name;
        bool loging_enable;

        public LogMutex(string _name, ref Logs.DebugLog _log, bool _loging_enable = true)
        {
            loging_enable = _loging_enable;
            log = _log;
            name = _name;
            mutex = new System.Threading.Mutex();
        }
        ~LogMutex()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                mutex.Close();
            }
        }
        public void Close()
        {
            Dispose();
        }
        public void ReleaseMutex()
        {
            if (loging_enable)
            {
                //log.Debug($"{name}.ReleaseMutex() [{log.StackTrace()}]");
                //log.Debug($"{name}.ReleaseMutex()");
            }
            mutex.ReleaseMutex();
        }
        public void WaitOne(string s)
        {
            if (loging_enable)
            {
                //log.Debug($"{name}.WaitOne() [{log.StackTrace()}]");
                //log.Debug($"{name}.WaitOne() {s}");
            }
            mutex.WaitOne();
        }
    }
}
