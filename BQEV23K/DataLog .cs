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
        List<string> lst_param = new List<string>{
            "Temperature",
            "Voltage", "Cell 1 Voltage", "Cell 2 Voltage","Cell 3 Voltage","Cell 4 Voltage",
            "Current",
            "Relative State of Charge", "Absolute State of Charge",
            "Run time To Empty","Average Time to Empty", "Average Time to Full", "Charging Current", "Charging Voltage",
            "QMax Passed Q","QMax Time",
            "DOD0 Passed Q","DOD0 Passed E","DOD0 Time",
            "Cell 1 QMax","Cell 2 QMax","Cell 3 QMax","Cell 4 QMax",
            "Cell 1 QMax DOD0","Cell 2 QMax DOD0","Cell 3 QMax DOD0","Cell 4 QMax DOD0",
            "Cell 1 Raw DOD","Cell 2 Raw DOD","Cell 3 Raw DOD","Cell 4 Raw DOD",
            "Cell 1 DOD0","Cell 2 DOD0","Cell 3 DOD0","Cell 4 DOD0",
            "Cell 1 DODEOC","Cell 2 DODEOC","Cell 3 DODEOC","Cell 4 DODEOC",
            "Cell 1 Grid","Cell 2 Grid","Cell 3 Grid","Cell 4 Grid",
            "LStatus","IT Status","Battery Status","Manufacturing Status","Operation Status A","Operation Status B",
            "Charging Status","Gauging Status",
            "Safety Status A+B", "Safety Status C+D",
            "Safety Alert A+B","Safety Alert C+D",
            "PF Status A+B","PF Status C+D",
            "PF Alert A+B","PF Alert C+D",
        };

        /// <summary>
        /// Constructor
        /// </summary>
        public DataLog_t(GaugeInfo gauge)
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
                string ChemID = gauge.GetDisplayValue("CHEM_ID");
                //ChemID = ChemID.Substring(0, ChemID.IndexOf('\0'));
                do {
                    DTS = DateTime.Now.ToString().Replace(':', '_');
                    NameFile = $"{DTS} Log_ChemID[{ChemID}].csv";
                } while (File.Exists(@"Logs\" + NameFile));
                NameGpcFile = $"{DTS} GpcLog_ChemID[{ChemID}].csv";

                string heads = "Time[s],DataTime,Info,";
                string heads_gpc = "Time[s],DataTime,";
                foreach (var prm in lst_param)
                {
                    heads += $"{ gauge.GetShortName(prm) },";
                    heads_gpc += $"{ gauge.GetShortName(prm) },";
                }
                heads = heads.Remove(heads.Length - 1);
                heads_gpc = heads_gpc.Remove(heads_gpc.Length - 1);

                using (StreamWriter writer = new System.IO.StreamWriter($@"Logs\{ NameFile }", true, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine($"Device Chemistry ID = {ChemID}");
                    writer.WriteLine(heads);
                }
                using (StreamWriter writer_gpc = new System.IO.StreamWriter($@"Logs\{ NameGpcFile }", true, System.Text.Encoding.UTF8))
                {
                    writer_gpc.WriteLine("ProcessingType=2");
                    writer_gpc.WriteLine("NumCellSeries=4");
                    writer_gpc.WriteLine("ElapsedTimeColumn=0");
                    writer_gpc.WriteLine("VoltageColumn=3");
                    writer_gpc.WriteLine("CurrentColumn=8");
                    writer_gpc.WriteLine("TemperatureColumn=2");
                    writer_gpc.WriteLine($"Device Chemistry ID = {ChemID}");
                    writer_gpc.WriteLine(heads_gpc);
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

                if(item[item.Length - 1] == ',')
                    item = item.Remove(item.Length - 1);
                item += "\r\n";

                Mutex.WaitOne();
                try
                {
                    
                    if(info == "gauge")
                    {
                        //using (StreamWriter writer_gpc = new StreamWriter($@"Logs\{ NameGpcFile }", true, System.Text.Encoding.UTF8))
                        {
                            //writer_gpc.WriteLineAsync($"{time},{dts},{item}");
                            File.AppendAllText($@"Logs\{ NameGpcFile }", $"{time},{dts},{item}");
                        }
                        info += ",";
                    }
                    //using (StreamWriter writer = new StreamWriter($@"Logs\{ NameFile }", true, System.Text.Encoding.UTF8))
                    //using (StreamWriter writer = new File.AppendAllText($@"Logs\{ NameFile }", true, System.Text.Encoding.UTF8))
                    {
                        File.AppendAllText($@"Logs\{ NameFile }", $"{time},{dts},{info}{item}");
                        //writer.WriteLineAsync($"{time},{dts},{info}{item}");
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
            string item = "";
            foreach (var prm in lst_param) {
                item += $"{ gauge.GetDisplayValue(prm) },";
            }
            //item = item.Remove(item.Length - 1) + "\r\n"; 

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
