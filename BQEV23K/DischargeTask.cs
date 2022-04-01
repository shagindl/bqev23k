﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQEV23K
{
    /// <summary>
    /// This class handles a single discharge task.
    /// </summary>
    public class DischargeTask : GenericTask
    {
        private const int TerminationHoldOffMilliseconds = 5000;
        private int terminateVoltage;
        private double currentDeisharge, current;
        private bool isCompleted = false;
        private DateTime startTime;

        #region Properties
        /// <summary>
        /// Get name of task.
        /// </summary>
        override public string Name
        {
            get
            {
                return "Discharge";
            }
        }

        /// <summary>
        /// Get description of task.
        /// </summary>
        override public string Description
        {
            get
            {
                return Name + " - TerminateVoltage = " + terminateVoltage.ToString("D") + " mV";
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
        public DischargeTask(int tv, double _current = 4.0)
        {
            startTime = DateTime.Now;
            terminateVoltage = tv;
            currentDeisharge = _current;
            current = -1.0;
        }

        /// <summary>
        /// Initialize discharge task.
        /// </summary>
        /// <returns>Initialization status.</returns>
        override public bool InitializeTask()
        {
            startTime = DateTime.Now;
            isCompleted = false;
            return true;
        }

        /// <summary>
        /// Check and return task completation status.
        /// </summary>
        /// <param name="gaugeInfo">Reference to device/gauge in use.</param>
        /// <returns>True when task completed, otherwise false.</returns>
        override public bool IsTaskComplete(GaugeInfo gaugeInfo)
        {
            // Check for termination voltage
            if (!isCompleted)
            {
                if ( DateTime.Now.Subtract(startTime).TotalMilliseconds >= TerminationHoldOffMilliseconds)
                {
                    if(gaugeInfo.Voltage < this.terminateVoltage)
                    {
                        isCompleted = true;
                    }
                }
            }
            return isCompleted;
        }
        override public void SetCurrent(object _m5010)
        {
            var m5010 = (M5010.MARK_5010)_m5010;

            if(current != currentDeisharge)
            {
                m5010.SetCurrent(currentDeisharge);

                current = currentDeisharge;
            }
        }
    }
}
