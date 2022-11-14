using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace BQEV23K
{
    /// <summary>
    /// This class creates a new data log for GPC cycle.
    /// </summary>
    public class GpcDataLog : IDisposable
    {
        private DateTime startTime;
        System.IO.StreamWriter writer_gpc;
        private LogMutex Mutex;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cellCount">Configured battery cell count.</param>
        public GpcDataLog(int cellCount, ref Logs.DebugLog _log)
        {
            Mutex = new LogMutex("GpcDataLog.Mutex", ref _log);

            startTime = DateTime.Now;
            Mutex.WaitOne($"GpcDataLog()");
            try
            {
                if(!Directory.Exists("GPC Results"))
                {
                    Directory.CreateDirectory("GPC Results");
                }

                if(!File.Exists(@"GPC Results\Config.txt"))
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(@"GPC Results\config.txt", false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("ProcessingType=2");
                        writer.WriteLine("NumCellSeries=" + cellCount.ToString());
                        writer.WriteLine("ElapsedTimeColumn=0");
                        writer.WriteLine("VoltageColumn=1");
                        writer.WriteLine("CurrentColumn=2");
                        writer.WriteLine("TemperatureColumn=3");
                    }
                }

                if (File.Exists(@"GPC Results\roomtemp_rel_dis_rel.csv"))
                {
                    File.Delete(@"GPC Results\roomtemp_rel_dis_rel.csv");
                }
                writer_gpc = new System.IO.StreamWriter(new BufferedStream(File.OpenWrite(@"GPC Results\roomtemp_rel_dis_rel.csv"), 1 * 1024 * 1024), System.Text.Encoding.UTF8, 1 * 1024 * 1024, false);
                {
                    writer_gpc.WriteLine("ElapsedTime,Voltage,AvgCurrent,Temperature");
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Mutex.ReleaseMutex();
        }
        ~GpcDataLog()
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
                if (writer_gpc != null)
                {
                    writer_gpc.Flush();
                    writer_gpc.Close();
                    writer_gpc = null;
                }
                Mutex.Close();
            }
        }

        /// <summary>
        /// Write new data line to log file.
        /// </summary>
        /// <param name="voltage">Battery voltage</param>
        /// <param name="current">Battery current</param>
        /// <param name="temperature">Battery temperature</param>
        public async void WriteLine(int voltage, int current, double temperature)
        {
            await Task.Run(() =>
            {
                Mutex.WaitOne($"WriteLine()");
                try
                {
                    if(writer_gpc != null)
                        writer_gpc.WriteLineAsync(DateTime.Now.Subtract(startTime).TotalSeconds.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")) + "," + voltage.ToString() + "," + current.ToString() + "," + temperature.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Mutex.ReleaseMutex();
            });
        }
    }
}
