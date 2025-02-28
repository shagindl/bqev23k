﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BQEV23K
{
    /// <summary>
    /// Cycle type to process
    /// </summary>
    public enum CycleType
    {
        None,
        LearningCycle,
        GpcCycle,
        ProductionCycle,
        DischargeChargeTask
    }

    /// <summary>
    /// Cycle mode type
    /// </summary>
    public enum CycleModeType
    {
        None,
        Manual,
        Automatic
    }

    public class LogWriteEventArgs : EventArgs
    {
        public LogWriteEventArgs(string s)
        {
            msg = s;
        }
        private string msg;
        public string Message
        {
            get { return msg; }
        }
    }

    /// <summary>
    /// This class controls and processes the entire learning or GPC cycle.
    /// </summary>
    public class Cycle : IDisposable
    {
        private const int ProcessTimerPeriodms = 1000;
        private List<GenericTask> taskList;
        private CancellationTokenSource cancelSource;
        private CancellationToken cancelToken;
        private DateTime startTime;
        private Timer cycleTimer;
        private bool cycleInProgress;
        private int currentTask;
        private bool processStatus;
        private GaugeInfo gauge;
        private M5010.MARK_5010 m5010;
        private TimeSpan elapsedTime;
        private Type taskType;
        private CycleModeType cycleModeType;
        private bool pushLoadStartButton = true;

        private Stopwatch stop_watch;

        public delegate void LogWriteDelegate(object sender, LogWriteEventArgs e);
        public event LogWriteDelegate LogWriteEvent;

        public event EventHandler CycleCompleted;
        protected virtual void OnCycleCompleted(EventArgs e)
        {
            CycleCompleted?.Invoke(this, e);
        }

        #region Properties
        /// <summary>
        /// Get elapsed cycle time.
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get
            {
                return elapsedTime;
            }
        }

        /// <summary>
        /// Get cycle in process status.
        /// </summary>
        /// <remarks>True when in progress, otherwise false.</remarks>
        public bool CycleInProgress
        {
            get
            {
                return cycleInProgress;
            }
        }

        /// <summary>
        /// Get name of the actual running task.
        /// </summary>
        public string RunningTaskName
        {
            get
            {
                return taskType.Name;
            }
        }

        /// <summary>
        /// Get or set cycle mode type.
        /// </summary>
        /// <remarks>Manual or Automatic</remarks>
        public CycleModeType CycleModeType
        {
            get
            {
                return cycleModeType;
            }

            set
            {
                cycleModeType = value;
            }
        }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_taskList">Task list to be cycled through.</param>
        /// <param name="_gauge">Gauge class object.</param>
        public Cycle(List<GenericTask> _taskList, GaugeInfo _gauge, M5010.MARK_5010 _m5010)
        {
            taskList = _taskList;
            gauge = _gauge;
            gauge.ToggleChargerRelay(false);
            gauge.ToggleLoadRelay(false);
            m5010 = _m5010;
            pushLoadStartButton = true;
        }

        /// <summary>
        /// Dispose managed resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            cancelSource.Dispose();
            cycleTimer.Dispose();
        }

        public void UpdateGauge(GaugeInfo _gauge)
        {
            gauge = _gauge;
        }

            /// <summary>
            /// Write new string to LogViewer control.
            /// </summary>
            /// <param name="log">String to write.</param>
            private void LogWrite(string log)
        {
            LogWriteEvent?.Invoke(this, new LogWriteEventArgs(log));
        }

        /// <summary>
        /// Start selected cycle.
        /// </summary>
        /// <returns>False on error. True if cycle is in process.</returns>
        public bool StartCycle()
        {
            stop_watch = new Stopwatch();

            if (taskList == null || taskList.Count == 0)
            {
                LogWrite("No task to run!");
                return false;
            }
            
            cancelSource = new CancellationTokenSource();
            cancelToken = cancelSource.Token;

            LogWrite("Learning cycle started...");
            currentTask = 0;

            GenericTask task = taskList[currentTask];
            taskType = task.GetType();
            LogWrite("Start task " + task.Description);
            try
            {
                cycleInProgress = task.InitializeTask();
                if(cycleInProgress)
                {
                    startTime = DateTime.Now;
                    StartTimer();
                }
                return cycleInProgress;
            } catch (Exception ex)
            {
                LogWrite("Error processing task. Details: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Start cycle processing timer.
        /// </summary>
        private void StartTimer()
        {
            cycleTimer = new Timer(processCycle, null, ProcessTimerPeriodms, ProcessTimerPeriodms);
        }

        /// <summary>
        /// Stop cycle processing timer.
        /// </summary>
        private void StopTimer()
        {
            cycleTimer.Dispose();
            if(!cancelSource.IsCancellationRequested)
                cancelSource.Cancel();
        }

        /// <summary>
        /// Cancel entire cycle.
        /// </summary>
        public void CancelCycle()
        {
            StopTimer();
            gauge.ToggleChargerRelay(false);
            gauge.ToggleLoadRelay(false);
            pushLoadStartButton = true;
            cycleInProgress = false;
            LogWrite("Learning cycle cancelled.");
        }

        /// <summary>
        /// Entire cycle proccessed succesfully.
        /// </summary>
        private void CycleComplete()
        {
            StopTimer();
            gauge.ToggleChargerRelay(false);
            gauge.ToggleLoadRelay(false);
            pushLoadStartButton = true;
            cycleInProgress = false;
            LogWrite("Learning cycle complete.");
        }

        /// <summary>
        /// Periodically called, asyncronous processing the cycle status.
        /// </summary>
        /// <param name="state">Not used</param>
        private async void processCycle(object state)
        {
            if (processStatus) return;
            processStatus = true;
            

            processStatus = await Task.Run(() =>
            {
                if (!cycleInProgress)
                    return false;

                if(gauge.HasSMBusError)
                    return false;

                if(!gauge.fValidInfo)
                    return false;

                if (currentTask < taskList.Count)
                {
                    GenericTask t = taskList[currentTask];
                    if (t.IsTaskComplete(gauge))
                    {
                        LogWrite("End task: " + t.Name + " completed in " + DateTime.Now.Subtract(t.StartTime).ToString(@"hh\:mm\:ss"));
                        LogWrite("LStatus: " + gauge.GetDisplayValue("LStatus"));

                        currentTask += 1;
                        if(currentTask >= taskList.Count)
                        {   // Entire cycle completed
                            cycleInProgress = false;
                            CycleComplete();
                            OnCycleCompleted(EventArgs.Empty);
                            return false;
                        }
                        GenericTask task = taskList[currentTask];
                        taskType = task.GetType();
                        LogWrite("Start task " + task.Description);

                        if (taskType.Name == "RelaxTask")
                            LogWrite("Relaxing...Please wait...");

                        cycleInProgress = task.InitializeTask();
                        if (!cycleInProgress)
                            LogWrite("Error processing task.");
                    } 
                    else if(taskType.Name == "ChargeTask")
                    {
                        if (gauge.Current == 0)
                        {
                            if (cycleModeType == BQEV23K.CycleModeType.Manual)
                            {
                                LogWrite("Charge Mode - Connect charger or power supply now.");
                            }
                            else
                            {
                                if (stop_watch.IsRunning && (stop_watch.ElapsedMilliseconds > 1000)) stop_watch.Stop();
                                if (!stop_watch.IsRunning)
                                {
                                    gauge.ToggleChargerRelay(true, true);
                                    gauge.ToggleLoadRelay(false);
                                    pushLoadStartButton = true;
                                    stop_watch.Start();
                                }
                            }
                        }
                        elapsedTime = DateTime.Now.Subtract(t.StartTime);
                    }
                    else if (taskType.Name == "RelaxTask")
                    {
                        RelaxTask r = (RelaxTask)t;
                        elapsedTime = r.EndTime.Subtract(DateTime.Now);
                        gauge.ToggleChargerRelay(false);
                        gauge.ToggleLoadRelay(false);
                        pushLoadStartButton = true;
                    }
                    else if (taskType.Name == "DischargeTask")
                    {
                        elapsedTime = DateTime.Now.Subtract(t.StartTime);

                        if (gauge.Current >= 0)
                        {
                            if (cycleModeType == BQEV23K.CycleModeType.Manual)
                            {
                                LogWrite("Discharge Mode - Connect load now.");
                            }
                            else
                            {
                                if (stop_watch.IsRunning && (stop_watch.ElapsedMilliseconds > 1000) ) stop_watch.Stop();

                                if (!stop_watch.IsRunning)
                                {
                                    gauge.ToggleChargerRelay(false);
                                    gauge.ToggleLoadRelay(true, true);
                                    if (pushLoadStartButton)
                                    {
                                        gauge.RemoteLoadStartButton();
                                        pushLoadStartButton = false;
                                    }
                                    stop_watch.Start();
                                }
                            }
                        }
                        else
                        {
                            if (elapsedTime.TotalSeconds > 0.5 * 60) {
                                if (m5010.IsConnected)
                                {
                                    taskList[currentTask].SetCurrent(m5010);
                                }
                            }
                        }
                        
                    }
                }
                return false;
            }, cancelToken);
        }
    }
}
