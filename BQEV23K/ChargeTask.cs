using System;

namespace BQEV23K
{
    /// <summary>
    /// This class handles a single charge task.
    /// </summary>
    public class ChargeTask : GenericTask
    {
        private const int TerminationHoldOffMilliseconds = 10000;
        private const int ChargeStartedCurrentThresholdMilliamps = 30;
        private int taperCurrent, EndHoldTime;
        private bool isCompleted = false;
        private bool currentHasStarted = false;
        private DateTime startTime, HoldTime;

        #region Properties
        /// <summary>
        /// Get name of task.
        /// </summary>
        override public string Name
        {
            get
            {
                return "Charge";
            }
        }

        /// <summary>
        /// Get description of task.
        /// </summary>
        override public string Description
        {
            get
            {
                return Name + " - TaperCurrent = " + taperCurrent.ToString("D") + " mA";
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
        /// <param name="tc">Taper current to end task, in [mA].</param>
        public ChargeTask(int tc, int end_hold_time = 0)
        {
            startTime = DateTime.Now;
            taperCurrent = tc;
            EndHoldTime = end_hold_time;
            HoldTime = startTime;
        }

        /// <summary>
        /// Initialize charge task.
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
            if (!isCompleted)
            {
                if(currentHasStarted)
                {
                    if (gaugeInfo.Current < taperCurrent && gaugeInfo.FlagFC == true)
                    {
                        if(HoldTime == startTime)
                        {
                            HoldTime = DateTime.Now;
                        }
                        if (DateTime.Now.Subtract(HoldTime).TotalSeconds >= EndHoldTime)
                        {
                            isCompleted = true;
                        }
                    }
                    else {
                        HoldTime = startTime;

                        if (DateTime.Now.Subtract(startTime).TotalMilliseconds >= TerminationHoldOffMilliseconds && gaugeInfo.Current < taperCurrent)
                        {
                            isCompleted = false;
                        }
                        else if (gaugeInfo.Current < taperCurrent && gaugeInfo.FlagFC == false)
                        {
                            isCompleted = false;
                        }
                    }
                }
                else if(DateTime.Now.Subtract(startTime).TotalMilliseconds >= TerminationHoldOffMilliseconds)
                {
                    if (gaugeInfo.Current > ChargeStartedCurrentThresholdMilliamps)
                    {
                        this.currentHasStarted = true;
                    }
                }
            }
            return isCompleted;
        }
   }
}
