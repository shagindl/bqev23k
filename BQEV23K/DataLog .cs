using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BQEV23K
{
    /// <summary>
    /// This class creates a new data log for GPC cycle.
    /// </summary>
    public class DataLog_t : IDisposable
    {
        private DateTime startTime;
        private string NameFile;
        private System.Threading.Mutex Mutex = new System.Threading.Mutex();

        /// <summary>
        /// Constructor
        /// </summary>
        public DataLog_t()
        {
            startTime = DateTime.Now;
            try
            {
                if(!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }

                do {
                    NameFile = DateTime.Now.ToString() + " Log.csv";
                    NameFile = NameFile.Replace(':', '_');
                } while (File.Exists(@"Logs\" + NameFile));

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(@"Logs\" + NameFile, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("DataTime,Type,Volt[mV],VoltC1[mV],VoltC2[mV],VoltC3[mV],VoltC4[mV],Current[mA], " +
                        "LStatus,IT Status[hex],Battery Status[hex],Manufacturing Status[hex],Operation Status A[hex]");
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            Mutex.Dispose();
        }

        /// <summary>
        /// Write new data line to log file.
        /// </summary>
        /// 
        public async void WriteLine(string item)
        {
            await Task.Run(() =>
            {
                try
                {
                    Mutex.WaitOne();
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(@"Logs\" + NameFile, true, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine(item);
                    }
                    Mutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    Mutex.ReleaseMutex();
                    Console.WriteLine(ex.Message);
                }
            });
        }

        public void WriteLine(GaugeInfo gauge)
        {
            var item = DateTime.Now.ToString() + "," + "gauge," +
                            gauge.Voltage.ToString() + "," +
                            gauge.GetDisplayValue("Cell 1 Voltage") + "," +
                            gauge.GetDisplayValue("Cell 2 Voltage") + "," +
                            gauge.GetDisplayValue("Cell 3 Voltage") + "," +
                            gauge.GetDisplayValue("Cell 4 Voltage") + "," +
                            gauge.Current.ToString() + "," +
                            gauge.GetDisplayValue("LStatus") + "," +
                            gauge.GetDisplayValue("IT Status") + "," +
                            gauge.GetDisplayValue("Battery Status") + "," +
                            gauge.GetDisplayValue("Manufacturing Status") + "," +
                            gauge.GetDisplayValue("Operation Status A");
            WriteLine(item);
        }
        //public async void WriteMessage(object sender, LogWriteEventArgs e)
        //{
        //    var item = DateTime.Now.ToString() + "," + e.Message;
        //    await Task.Run(() =>
        //    {
        //        WriteLine(item);
        //    });
        //}
        public void WriteMessage(object sender, LogWriteEventArgs e)
        {
            var item = DateTime.Now.ToString() + "," + e.Message;
            WriteLine(item);
        }
    }
}
