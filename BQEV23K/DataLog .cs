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
        private string NameFile, NameGpcFile;
        private System.Threading.Mutex Mutex = new System.Threading.Mutex();
        //System.IO.StreamWriter writer, writer_gpc;

        /// <summary>
        /// Constructor
        /// </summary>
        public DataLog_t()
        {
            startTime = DateTime.Now;
            Mutex.WaitOne();
            try
            {
                if(!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }
                string DTS = "";
                do {
                    DTS = DateTime.Now.ToString().Replace(':', '_');
                    NameFile = $"{DTS} Log.csv";
                } while (File.Exists(@"Logs\" + NameFile));
                NameGpcFile = $"{DTS} GpcLog.csv";

                using (StreamWriter writer = new System.IO.StreamWriter($@"Logs\{ NameFile }", true, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("Time[s],DataTime,Info,Temperature °C,Volt[mV],VoltC1[mV],VoltC2[mV],VoltC3[mV],VoltC4[mV],Current[mA]," +
                        "RSOC[%],ASOC[%]," +
                        "LStatus,IT Status[hex],Battery Status[hex],Manufacturing Status[hex],Operation Status A[hex],Operation Status B[hex]," + 
                        "Safety Status A+B[hex],Safety Status C+D[hex]");
                }
                using (StreamWriter writer_gpc = new System.IO.StreamWriter($@"Logs\{ NameGpcFile }", true, System.Text.Encoding.UTF8))
                {
                    writer_gpc.WriteLine("ProcessingType=2");
                    writer_gpc.WriteLine("NumCellSeries=4");
                    writer_gpc.WriteLine("ElapsedTimeColumn=0");
                    writer_gpc.WriteLine("VoltageColumn=3");
                    writer_gpc.WriteLine("CurrentColumn=8");
                    writer_gpc.WriteLine("TemperatureColumn=2");
                    writer_gpc.WriteLine("Time[s],DataTime,Temperature °C,Volt[mV],VoltC1[mV],VoltC2[mV],VoltC3[mV],VoltC4[mV],Current[mA]," +
                        "RSOC[%],ASOC[%]," +
                        "LStatus,IT Status[hex],Battery Status[hex],Manufacturing Status[hex],Operation Status A[hex],Operation Status B[hex]," +
                        "Safety Status A+B[hex],Safety Status C+D[hex]");
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Mutex.ReleaseMutex();
        }
        ~DataLog_t()
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
                Mutex.WaitOne();
                //if (writer != null)
                //{
                //    writer.Flush();
                //    writer.Close();
                //    writer.Dispose();
                //}
                //if (writer_gpc != null)
                //{
                //    writer_gpc.Flush();
                //    writer_gpc.Close();
                //    writer_gpc.Dispose();
                //}
                Mutex.ReleaseMutex();
            }
        }
        /// <summary>
        /// Write new data line to log file.
        /// </summary>
        /// 
        public void WriteLine(string info, string item)
        {
            Task.Run(() =>
            {
                string time = DateTime.Now.Subtract(startTime).TotalSeconds.ToString("F1", CultureInfo.CreateSpecificCulture("en-US"));
                string dts = DateTime.Now.ToString();

                Mutex.WaitOne();
                try
                {
                    
                    if(info == "gauge")
                    {
                        using (StreamWriter writer_gpc = new StreamWriter($@"Logs\{ NameGpcFile }", true, System.Text.Encoding.UTF8))
                        {
                            writer_gpc.WriteLineAsync($"{time},{dts},{item}");
                        }
                        info += ",";
                    }
                    using (StreamWriter writer = new StreamWriter($@"Logs\{ NameFile }", true, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLineAsync($"{time},{dts},{info}{item}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Mutex.ReleaseMutex();
            });
        }

        public void WriteLine(GaugeInfo gauge)
        {
            gauge.ReadDeviceMutex.WaitOne();
            var item = gauge.Temperature.ToString().Replace(',','.') + "," +
                            gauge.Voltage.ToString() + "," +
                            gauge.GetDisplayValue("Cell 1 Voltage") + "," +
                            gauge.GetDisplayValue("Cell 2 Voltage") + "," +
                            gauge.GetDisplayValue("Cell 3 Voltage") + "," +
                            gauge.GetDisplayValue("Cell 4 Voltage") + "," +
                            gauge.Current.ToString() + "," +
                            gauge.GetDisplayValue("Relative State of Charge") + "," +
                            gauge.GetDisplayValue("Absolute State of Charge") + "," +
                            gauge.GetDisplayValue("LStatus") + "," +
                            gauge.GetDisplayValue("IT Status") + "," +
                            gauge.GetDisplayValue("Battery Status") + "," +
                            gauge.GetDisplayValue("Manufacturing Status") + "," +
                            gauge.GetDisplayValue("Operation Status A") + "," +
                            gauge.GetDisplayValue("Operation Status B") + "," +
                            gauge.GetDisplayValue("Safety Status A+B") + "," +
                            gauge.GetDisplayValue("Safety Status C+D");
            gauge.ReadDeviceMutex.ReleaseMutex();
            WriteLine("gauge", item);
        }

        public void WriteMessage(object sender, LogWriteEventArgs e)
        {
            WriteLine("info:", e.Message);
        }
        public void WriteMessage(string mess)
        {
            WriteLine("info:", mess);
        }
    }
}
