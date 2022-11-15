using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

#nullable enable

namespace BQEV23K
{
    public class CTask
    {
        public string? Name { get; set; }
        public string? Mode { get; set; }
        public int? RSOC { get; set; }
        public int? Pause { get; set; }
        public int? terminateVoltage { get; set; }
        public int? termVoltageCell { get; set; }
        public int? taperCurrent { get; set; }
    }

    public class CustomTaskParam {
        public IList<CTask>? ListTasks { get; set; }
    }


    public class CustomTask : GenericTask
    {
        private DateTime startTime, endTime, holdTime, PauseTime;
        private bool isCompleted = false;
        private CustomTaskParam LTask;
        private int itemTask = 0, itemTaskPrev = 0;
        private bool fBeginTaskComplete = true, fBeginTask = false, fTaskComplited = false;
        private bool fPauseTime = false;

        #region Properties
        /// <summary>
        /// Get name of task.
        /// </summary>
        override public string Name
        {
            get
            {
                return "CustomCycle";
            }
        }

        /// <summary>
        /// Get description of task.
        /// </summary>
        override public string Description
        {
            get
            {
                return Name;
            }
        }

        /// <summary>
        /// Get start time of task.
        /// </summary>
        override public DateTime StartTime
        {
            get
            {
                return startTime;
            }
        }
        #endregion
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tv">Termination voltage to end task.</param>
        public CustomTask()
        {
            string jsonString = File.ReadAllText(@"Resources\CustomTask.json");
            LTask = JsonSerializer.Deserialize<CustomTaskParam>(jsonString)!;
        }
        /// <summary>
        /// Check and return task completation status.
        /// </summary>
        /// <param name="gaugeInfo">Reference to device/gauge in use.</param>
        /// <returns>True when task completed, otherwise false.</returns>
        override public bool IsTaskComplete(GaugeInfo gauge)
        {
            if (isCompleted) return isCompleted;

            // -- End CustomTask
            if (itemTask >= LTask.ListTasks!.Count)
            {
                isCompleted = true;
                return isCompleted;
            }
            CTask task = LTask.ListTasks![itemTask];
            // -- Begin CustomTask
            if (fBeginTaskComplete) {
                fBeginTaskComplete = false;

                fBeginTask = true;
                startTime = DateTime.Now;

                LogView("Start task " + task.Name!);
            }
            // -- Next Task
            if(itemTask != itemTaskPrev)
            {
                CTask prev_task = LTask.ListTasks![itemTask - 1];
                LogView("End task: " + prev_task.Name + " completed in " + DateTime.Now.Subtract(startTime).ToString(@"hh\:mm\:ss"));
                LogView("Start task " + task.Name);
                if (task.Name == "RelaxTask")
                    LogView("Relaxing...Please wait...");

                fBeginTask = true;
                fTaskComplited = false;
                startTime = DateTime.Now;
                itemTaskPrev = itemTask;
            }
            // -- Work Tasks
            if (task.Mode == @"DischargeTask")
            {
                if (fBeginTask)
                {
                    fBeginTask = false;
                    holdTime = DateTime.Now;
                    gauge.ToggleChargerRelay(false);
                    gauge.ToggleLoadRelay(true, true);
                }

                if (!fTaskComplited)
                    fTaskComplited = CheckedTermCond(gauge.RSOC, task.RSOC, gauge);
                if (!fTaskComplited)
                    fTaskComplited = CheckedTermCond(gauge.Voltage, task.terminateVoltage, gauge);
                if (!fTaskComplited)
                    fTaskComplited = CheckedTermCond(gauge.MinVoltageCell, task.termVoltageCell, gauge);

                if (!fTaskComplited && gauge.Current >= 0 && DateTime.Now.Subtract(holdTime).TotalMilliseconds >= 1000)
                {
                    gauge.ToggleChargerRelay(false);
                    gauge.ToggleLoadRelay(true, true);
                    holdTime = DateTime.Now;
                }
            }
            else if (task.Mode == @"PauseTask")
            {
                if (fBeginTask)
                {
                    fBeginTask = false;
                    endTime = DateTime.Now;
                    endTime = endTime.AddMinutes((double)task.Pause!);
                    // --
                    gauge.ToggleChargerRelay(false);
                    gauge.ToggleLoadRelay(false, false);
                }
                if (DateTime.Compare(DateTime.Now, endTime) > 0)
                {
                    itemTask++;
                    fTaskComplited = true;
                }
            }
            else if (task.Mode == @"ChargeTask")
            {
                if (fBeginTask)
                {
                    fBeginTask = false;
                    holdTime = PauseTime = DateTime.Now;
                    gauge.ToggleChargerRelay(true, true);
                    gauge.ToggleLoadRelay(false);
                }

                if (gauge.Current <= 0 && DateTime.Now.Subtract(holdTime).TotalMilliseconds >= 1000)
                {
                    holdTime = DateTime.Now;
                    gauge.ToggleChargerRelay(true, true);
                    gauge.ToggleLoadRelay(false);
                }
                if(fPauseTime && DateTime.Now.Subtract(PauseTime).TotalMilliseconds >= 10000)
                {
                    fPauseTime = true;
                }
                else if (fPauseTime)
                {
                    if (gauge.Current < task.taperCurrent && gauge.FlagFC)
                    {
                        itemTask++;
                        gauge.ToggleChargerRelay(false);
                        gauge.ToggleLoadRelay(false, false);
                    }
                }
            }
            else
            {
                LogView("Error: for task = " + task.Name + "no defined handler.");
            }

            return isCompleted;
        }
        bool CheckedTermCond(int val, int? threshold, GaugeInfo gauge)
        {
            if (threshold is not null)
            {
                if (val <= threshold)
                {
                    itemTask++;
                    // --
                    gauge.ToggleChargerRelay(false);
                    gauge.ToggleLoadRelay(false, false);
                    return true;
                }
            }

            return false;
        }
    }
}
