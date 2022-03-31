using System;
using System.Globalization;
using System.IO;

namespace BQEV23K
{
    /// <summary>
    /// This class creates a new data log for GPC cycle.
    /// </summary>
    public class DataLog_t
    {
        private DateTime startTime;
        private string NameFile;

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
                    writer.WriteLine("DataTime, Type, Voltage[mV], Current[mA], " +
                        "LStatus, IT Status[hex], Battery Status[hex], Manufacturing Status[hex], Operation Status A[hex]");
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Write new data line to log file.
        /// </summary>
        /// 
        public void WriteLine(string item)
        {
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(@"Logs\" + NameFile, true, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void WriteLine(GaugeInfo gauge)
        {
            var item = DateTime.Now.ToString() + "," + "gauge," +
                            gauge.Voltage.ToString() + "," +
                            gauge.Current.ToString() + "," +
                            gauge.GetDisplayValue("LStatus") + "," +
                            gauge.GetDisplayValue("IT Status") + "," +
                            gauge.GetDisplayValue("Battery Status") + "," +
                            gauge.GetDisplayValue("Manufacturing Status") + "," +
                            gauge.GetDisplayValue("Operation Status A");
            WriteLine(item);
        }
        public void WriteMessage(object sender, LogWriteEventArgs e)
        {
            var item = DateTime.Now.ToString() + "," + e.Message;
            WriteLine(item);
        }
    }
}
