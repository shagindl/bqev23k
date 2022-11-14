using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using NLog;
using NLog.Targets;
using NLog.Layouts;

namespace Logs
{
    public class DebugLog : IDisposable
    {
        private Regex regex, regex_file;
        private Logger Inst;
        private DateTime InitialTime, startTime;
        private Mutex mtx;

        public Logger inst
        {
            get
            {
                mtx.WaitOne();
                if (DateTime.Now.Subtract(startTime).TotalMinutes > 3 * 60)
                {
                    NewFileLog();
                }
                mtx.ReleaseMutex();

                return Inst;
            }
        }


        public DebugLog()
        {
            regex = new Regex(@"((BQEV23K).+\))");
            regex_file = new Regex(@"BQEV23K.+$");
            mtx = new Mutex();
            InitialTime = DateTime.Now;

            NewFileLog();
        }
        ~DebugLog()
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
                mtx.ReleaseMutex();
                mtx.Close();
            }
        }

        private void NewFileLog() { 

            startTime = DateTime.Now;
            string DTS = startTime.ToString().Replace(':', '_');
            string InitialDTS = InitialTime.ToString().Replace(':', '_');

            FileTarget target = new FileTarget();
            target.FileName = "${basedir}/Logs/" + $"{InitialDTS} DebugLog/" + $"{DTS} DebugLog.csv";

            CsvLayout layout = new CsvLayout();

            layout.Columns.Add(new CsvColumn("time", "${longdate}"));
            layout.Columns.Add(new CsvColumn("message", "${message}"));
            layout.Columns.Add(new CsvColumn("logger", "${logger}"));
            layout.Columns.Add(new CsvColumn("level", "${level}"));

            target.Layout = layout;

            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Debug);

            Inst = LogManager.GetLogger("DebugLog");
            Inst.Debug("Start DebugLog!");
        }
        public void Debug(string s)
        {
            //inst.Debug(s);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string StackTrace()
        {
            var trace = System.Environment.StackTrace;
            MatchCollection matches = regex.Matches(trace);
            var toarray = from Match match in matches select match.Value;

            return string.Join("\n   ", toarray);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int __LINE__([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return lineNumber;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string __FILE__([System.Runtime.CompilerServices.CallerFilePath] string fileName = "")
        {
            return regex_file.Matches(fileName)[0].Value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string __FL__([System.Runtime.CompilerServices.CallerFilePath] string fileName = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return $"[{regex_file.Matches(fileName)[0].Value}({lineNumber})]";
        }
        
    }
}
