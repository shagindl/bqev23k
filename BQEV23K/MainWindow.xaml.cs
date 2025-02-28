﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;

/**
 * How to setup such project see:
 * http://e2e.ti.com/support/power_management/battery_management/f/180/p/640114/2363362#2363362
 *   
 * Download EV2300 customer kit:
 * https://e2e.ti.com/support/power_management/battery_management/f/180/p/671348/2470529#2470529
 * 
 */

namespace BQEV23K
{
    /// <summary>
    /// Main window class
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private const int CmdExecDelayMilliseconds = 4000;
        private const int ResetCmdExecDelayMilliseconds = 8000;
        private PlotViewModel plot;
        private EV23K board;
        private M5010.MARK_5010 Mark5010;
        Thread ThreadConnectionM5010;
        CancellationTokenSource ctConnectionM5010;
        private GaugeInfo gauge;
        private DispatcherTimer timerUpdateGUI;
        Thread ThreadUpdatePlot;
        CancellationTokenSource ctUpdatePlot = new CancellationTokenSource();
        Thread ThreadDataLog;
        CancellationTokenSource ctDataLog = new CancellationTokenSource();
        private Cycle cycle;
        private CycleType selectedCycleType = CycleType.None;
        private CycleModeType selectedCycleModeType = CycleModeType.None;
        private GpcDataLog gpcLog;
        private DataLog_t DataLog;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            plot = new PlotViewModel();
            DataContext = plot;

            Title = @"BQEV2400 - v2.5.0.0 by ""ООО ВЗОР"" /Mictronics";
            System.Windows.Forms.Integration.WindowsFormsHost host;
            board = new EV23K(out host);
            host.Width = host.Height = 0;
            host.IsEnabled = false;
            MainGrid.Children.Add(host);

            Mark5010 = new M5010.MARK_5010();
            // -- Connection
            ctConnectionM5010 = new CancellationTokenSource();
            ThreadConnectionM5010 = new Thread(TaskConnectionM5010);
            ThreadConnectionM5010.Start(ctConnectionM5010.Token);

            timerUpdateGUI = new DispatcherTimer();
            timerUpdateGUI.Tick += new EventHandler(UpdateGui);
            timerUpdateGUI.Interval = new TimeSpan(0, 0, 0, 1, 0);
            fInUpdateGui = false;
            timerUpdateGUI.Start();
        }

        /// <summary>
        /// Initialize when main window was loaded.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CfgCycleCellCount.Text = Properties.Settings.Default.CellCount;
            CfgCycleTaperCurr.Text = Properties.Settings.Default.TaperCurrent;
            CfgCycleTermVolt.Text = Properties.Settings.Default.TerminationVoltage;
            CfgCycleTermVoltCell.Text = Properties.Settings.Default.TermVoltageCell;
            CfgCycleChargeRelaxHours.Text = Properties.Settings.Default.ChargeRelaxHours;
            CfgCycleDischargeRelaxHours.Text = Properties.Settings.Default.DischargeRelaxHours;
            IP_Load.Text = Properties.Settings.Default.IP_Load;
            CycleRepetitions.Text = Properties.Settings.Default.CycleRepetitions.ToString();

            try
            {
                int BrdsNumber = 0;
                string BrdsName = "";

                if (board.Connect(out BrdsNumber, out BrdsName) != 0)
                    throw new ArgumentException("EV2300 not found.");

                LogView.AddEntry("EV2300 connected...");
                Console.WriteLine(BrdsName);

                EV23KError err = board.CheckForError();
                gauge = new GaugeInfo(board);

                UpdateGui(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Clean up when main window will be closed.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // -- Disable All
            board.DisableAll();

            if (gpcLog != null)
            {
                gpcLog.Dispose();
            }
            if (cycle != null)
                cycle.CancelCycle();

            if(gauge != null)
            {
                gauge.ReadDeviceMutex.WaitOne();
                gauge.StopPolling();
                gauge.ReadDeviceMutex.ReleaseMutex();
            }
            if (DataLog != null)
            {
                LodDeletEvent();
                DataLog.Dispose();
            }
            // -- All Stop
            timerUpdateGUI.Stop();
            ctConnectionM5010.Cancel();
            ctUpdatePlot.Cancel();
            ctDataLog.Cancel();

            if(ThreadConnectionM5010 != null)
                ThreadConnectionM5010.Join();
            if (ThreadUpdatePlot != null)
                ThreadUpdatePlot.Join();
            if (ThreadDataLog != null)
                ThreadDataLog.Join();

            board.Disconnect();
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
            if (disposing)
            {

                ctConnectionM5010.Cancel();
                ctUpdatePlot.Cancel();
                ctDataLog.Cancel();

                if (gpcLog != null)
                {
                    gpcLog.Dispose();
                }
                if (gauge != null)
                {
                    gauge.Dispose();
                }
                if (cycle != null)
                    cycle.Dispose();
                if (DataLog != null)
                {
                    LodDeletEvent();
                    DataLog.Dispose();
                }
                board.Dispose();
            }
        }

        /// <summary>
        /// Priodically update GUI elements.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used</param>
        bool fInUpdateGui = false, fSingleUpdateGui = false;
        private Stopwatch ReconnectBoard_StopWatch = new Stopwatch();
        public void UpdateGui(object sender, System.EventArgs e)
        {
            if (fInUpdateGui) return;

            fInUpdateGui = true;
            if (board != null && !board.IsDisposed)
            {
                if (board.IsPresent)
                {

                    IconEV23K.Source = GetImageSourceFromResource("usb-icon_128.png");
                    LabelEV23KName.Content = board.Name;
                }
                else
                {
                    if (gauge != null)
                    {
                        gauge.ReadDeviceMutex.WaitOne();
                        gauge.StopPolling();
                        gauge.ReadDeviceMutex.ReleaseMutex();
                        gauge.Dispose();
                    }
                    else
                    {
                        board.Disconnect();
                        board.Dispose();
                    }
                    LogView.AddEntry("EV2300 disconnected...");

                    ReconnectBoard_StopWatch.Start();
                    //int BrdsNumber = 0;
                    //string BrdsName = "";
                    //// -- Disconnect
                    //board.Disconnect();
                    //LogView.AddEntry("EV2300 disconnect.");
                    //// -- Connect
                    //board.Connect(out BrdsNumber, out BrdsName);
                    //LogView.AddEntry("EV2300 connected...");
                    //Console.WriteLine(BrdsName);
                    //EV23KError err = board.CheckForError();

                    //if (!board.IsPresent)
                    //{
                    //    IconEV23K.Source = GetImageSourceFromResource("USB-Disabled-128.png");
                    //    LabelEV23KName.Content = string.Empty;
                    //}
                    //else
                    //{
                    //    Console.WriteLine(BrdsName);
                    //}

                    IconEV23K.Source = GetImageSourceFromResource("USB-Disabled-128.png");
                    LabelEV23KName.Content = string.Empty;
                }
            }
            else
            {
                if (ReconnectBoard_StopWatch.ElapsedMilliseconds > 1000)
                {
                    ReconnectBoard_StopWatch.Restart();

                    System.Windows.Forms.Integration.WindowsFormsHost host;
                    board = new EV23K(out host);

                    try
                    {
                        int BrdsNumber = 0;
                        string BrdsName = "";

                        if (board.Connect(out BrdsNumber, out BrdsName) != 0)
                            throw new ArgumentException("EV2300 not found.");

                        LogView.AddEntry("EV2300 connected...");
                        Console.WriteLine(BrdsName);

                        EV23KError err = board.CheckForError();
                        gauge = new GaugeInfo(board);

                        if (cycle.CycleInProgress)
                        {
                            cycle.UpdateGauge(gauge);
                        }

                        ReconnectBoard_StopWatch.Stop();
                    }
                    catch (Exception ex)
                    {
                        board.Dispose();
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            if (gauge != null)
            {
                if (gauge.HasSMBusError)
                {
                    fSingleUpdateGui = false;
                    LabelGaugeChemID.Content = string.Empty;
                    RunTimeEmty.Content = string.Empty;
                    AvgTimeEmty.Content = string.Empty;
                    AvgTimeFull.Content = string.Empty;
                    ChgCurr.Content = string.Empty;
                    ChgVolt.Content = string.Empty;
                    LStatus.Content = string.Empty;
                    RSOC.Content = string.Empty;

                    IconArrows.Source = GetImageSourceFromResource("Arrows-Disabled-48.png");
                    IconGauge.Source = GetImageSourceFromResource("Gauge-Disabled-128.png");
                    LabelGaugeName.Content = LabelGaugeVersion.Text = string.Empty;
                    LabelGaugeVoltage.Content = LabelGaugeTemperature.Content = string.Empty;

                    LearningVoltageLabel.Content = "Voltage: ";
                    LearningCurrentLabel.Content = "Current: ";
                    LearningTemperatureLabel.Content = "Temperature: ";
                    LearningVoltage1Label.Content = "Voltage C1: ";
                    LearningVoltage2Label.Content = "Voltage C2: ";
                    LearningVoltage3Label.Content = "Voltage C3: ";
                    LearningVoltage4Label.Content = "Voltage C4: ";
                }
                else
                {
                    if (!fSingleUpdateGui)
                    {
                        LabelGaugeChemID.Content = $"CHEM_ID: {gauge.GetDisplayValue("CHEM_ID")}";
                    }

                    IconArrows.Source = GetImageSourceFromResource("Arrows-48.png");
                    IconGauge.Source = GetImageSourceFromResource("Gauge-128.png");
                    LabelGaugeName.Content = gauge.GetDisplayValue("Device Name");

                    string s = gauge.GetDisplayValue("DEVICE_NUMBER") + "_";
                    s += gauge.GetDisplayValue("HW_VERSION").Replace("0", "") + "_";
                    s += gauge.GetDisplayValue("FW_VERSION").Replace("0", "") + "_";
                    s += gauge.GetDisplayValue("FW_BUILD").Replace("0", "");
                    LabelGaugeVersion.Text = s;

                    LabelGaugeVoltage.Content = $"VBat: {gauge.GetDisplayValue("Voltage")} mV";
                    LabelGaugeTemperature.Content = $"Temp: { gauge.GetDisplayValue("Temperature")} ºС";
                    RunTimeEmty.Content = $"RunTimeEmty: {gauge.GetDisplayValue("Run time To Empty")} min";
                    AvgTimeEmty.Content = $"AvgTimeEmty: {gauge.GetDisplayValue("Average Time to Empty")} min";
                    AvgTimeFull.Content = $"AvgTimeFull: {gauge.GetDisplayValue("Average Time to Full")} min";
                    ChgCurr.Content = $"ChgCurr: {gauge.GetDisplayValue("Charging Current")} mA";
                    ChgVolt.Content = $"ChgVolt: {gauge.GetDisplayValue("Charging Voltage")} mV";
                    LStatus.Content = $"LStatus: {gauge.GetDisplayValue("LStatus")}";
                    RSOC.Content = $"RSOC: {gauge.GetDisplayValue("Relative State of Charge")}";

                    FlagFC.IsChecked = gauge.FlagFC;
                    FlagFD.IsChecked = gauge.FlagFD;
                    FlagGAUGE_EN.IsChecked = gauge.FlagGAUGE_EN;
                    FlagQEN.IsChecked = gauge.FlagQEN;
                    FlagQMAX.IsChecked = gauge.FlagQMAX;
                    FlagVDQ.IsChecked = gauge.FlagVDQ;
                    FlagRDIS.IsChecked = gauge.FlagRDIS;
                    FlagREST.IsChecked = gauge.FlagREST;
                    FlagVOK.IsChecked = gauge.FlagVOK;
                    FlagCHG.IsChecked = gauge.FlagCHG;
                    FlagDSG.IsChecked = gauge.FlagDSG;
                    FlagFET_EN.IsChecked = gauge.FlagFET_EN;
                    FlagOCVFR.IsChecked = gauge.FlagOCV;

                    CfgBattChemID.Text = gauge.GetDisplayValue("CHEM_ID");
                    CfgBattDesignVoltage.Text = gauge.DFDesignVoltage;
                    CfgBattDesignCapacity.Text = gauge.DFDesignCapacity;
                    CfgBattCellCount.Text = gauge.DFCellCount;
                    CfgBattTermVolt.Text = gauge.DFTermVoltage;
                    CfgBattTaperCurr.Text = gauge.DFTaperCurrent;
                    CfgBattChgCurrThres.Text = gauge.DFChgCurrentThreshold;
                    CfgBattDsgCurrThres.Text = gauge.DFDsgCurrentThreshold;

                    if (cycle != null && cycle.CycleInProgress)
                    {

                        LearningVoltageLabel.Content = "Voltage: " + gauge.GetDisplayValue("Voltage");
                        LearningCurrentLabel.Content = "Current: " + gauge.GetDisplayValue("Current");
                        LearningTemperatureLabel.Content = "Temperature: " + gauge.GetDisplayValue("Temperature");
                        LearningVoltage1Label.Content = "Voltage C1: " + gauge.GetDisplayValue("Cell 1 Voltage");
                        LearningVoltage2Label.Content = "Voltage C2: " + gauge.GetDisplayValue("Cell 2 Voltage");
                        LearningVoltage3Label.Content = "Voltage C3: " + gauge.GetDisplayValue("Cell 3 Voltage");
                        LearningVoltage4Label.Content = "Voltage C4: " + gauge.GetDisplayValue("Cell 4 Voltage");

                        if (cycle.RunningTaskName == "RelaxTask")
                        {
                            LearningTimeLabel.Content = "Waiting Time: " + cycle.ElapsedTime.ToString(@"hh\:mm\:ss");
                        }
                        else
                        {
                            LearningTimeLabel.Content = "Elapsed Time: " + cycle.ElapsedTime.ToString(@"hh\:mm\:ss");
                        }
                    }
                }
            }

            fInUpdateGui = false;
        }

        /// <summary>
        /// Update voltage, current and temperature plot.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private bool InUpdatePlot = false;
        public void UpdatePlot(object _ct)
        {
            var ct = (CancellationToken)_ct;

            try
            {
                do
                {
                    if (InUpdatePlot) return;

                    InUpdatePlot = true;

                    if (gauge != null)
                    {
                        gauge.ReadDeviceMutex.WaitOne();
                        var Voltage = gauge.Voltage;
                        var Current = gauge.Current;
                        var Temperature = gauge.Temperature;
                        gauge.ReadDeviceMutex.ReleaseMutex();

                        plot.Output(Voltage, Current, Temperature);

                        if (selectedCycleType == CycleType.GpcCycle && gpcLog != null)
                        {
                            gpcLog.WriteLine(Voltage, Current, Temperature);
                        }
                    }
                    InUpdatePlot = false;

                    Task.Delay(5000, ct).Wait();
                } while (!ct.IsCancellationRequested);
            }
            catch (Exception)
            {

            }
        }
        //public void UpdateDataLog(object sender, System.EventArgs e)
        public void UpdateDataLog(object _ct)
        {
            var ct = (CancellationToken)_ct;

            try
            {
                do
                {
                    if (DataLog != null)
                    {
                        DataLog.WriteLine(gauge);
                    }

                    Task.Delay(1000, ct).Wait();
                } while (!ct.IsCancellationRequested);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Get image from assembly ressource
        /// </summary>
        /// <param name="resourceName">Image resource name to get.</param>
        /// <returns>ImageSource from resource name.</returns>
        static internal ImageSource GetImageSourceFromResource(string resourceName)
        {
            Uri oUri = new Uri("pack://application:,,,/" + "BQEV23K" + ";component/Resources/" + resourceName, UriKind.RelativeOrAbsolute);
            return BitmapFrame.Create(oUri);
        }
        void LogAddEvent()
        {
            DataLog = new DataLog_t(gauge, $"{CycleType.ProductionCycle}");
            if(gauge != null)
                gauge.LogWriteEvent += DataLog.WriteMessage;
            if (LogView != null)
                LogView.LogWriteEvent += DataLog.WriteMessage;
            if (board != null)
                board.LogWriteEvent += DataLog.WriteMessage;
            if (Mark5010 != null)
                Mark5010.LogWriteEvent += DataLog.WriteMessage;
        }
        void LodDeletEvent()
        {
            if (gauge != null)
                gauge.LogWriteEvent -= DataLog.WriteMessage;
            if (LogView != null)
                LogView.LogWriteEvent -= DataLog.WriteMessage;
            if (board != null)
                board.LogWriteEvent -= DataLog.WriteMessage;
            if (Mark5010 != null)
                Mark5010.LogWriteEvent -= DataLog.WriteMessage;
        }
        /// <summary>
        /// Start new learning or GPC cycle on button click.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private async void ButtonCycleStart_Click(object sender, RoutedEventArgs e)
        {
            int cellCount = 0;
            int.TryParse(CfgCycleCellCount.Text, out cellCount);

            LogAddEvent();

            if (cellCount <= 0 || cellCount > 7)
            {
                LogView.AddEntry("Invalid cell count!");
                return;
            }

            int termVoltage = 0, termVoltageCell = 0;
            int.TryParse(CfgCycleTermVolt.Text, out termVoltage);
            int.TryParse(CfgCycleTermVoltCell.Text, out termVoltageCell);

            int ctv = (termVoltage / cellCount);
            if (termVoltage <= 0 || (ctv < 2000) || (termVoltage / cellCount > 4200))
            {
                LogView.AddEntry("Invalid termination voltage! (" + ctv.ToString() + "mV/cell)");
                return;
            }
            if (termVoltageCell < 2000 || termVoltageCell > 4200)
            {
                LogView.AddEntry("Invalid termination voltage cell! (" + ctv.ToString() + "mV/cell)");
                return;
            }

            int taperCurrent = 0;
            if (!int.TryParse(CfgCycleTaperCurr.Text, out taperCurrent))
            {
                taperCurrent = 100; // mA
                CfgCycleTaperCurr.Text = taperCurrent.ToString("D");
            }

            if (taperCurrent <= 0)
            {
                LogView.AddEntry("Invalid taper current!");
                return;
            }
            /* See TI SLUA848, chapter 4.2.1 */
            LogView.AddEntry("Preparing...");
            bool FlagFET_EN_valid = false;
            if (selectedCycleType == CycleType.DischargeChargeTask)
                FlagFET_EN_valid = true;
            else
                FlagFET_EN_valid = false;

            /* Clear FET_EN to be able to set charge and discharge FETs */
            if (gauge.FlagFET_EN != FlagFET_EN_valid)
            {
                gauge.CommandToogleFETenable();
            }
            await Task.Delay(CmdExecDelayMilliseconds);
            if (gauge.FlagFET_EN != FlagFET_EN_valid)
            {
                LogView.AddEntry("Failed to clear FET_EN.");
                return;
            }
            // --
            if (selectedCycleType == CycleType.DischargeChargeTask)
            {

            }
            else if (selectedCycleType == CycleType.ProductionCycle || selectedCycleType == CycleType.LearningCycle) { 
                /* Enable gauging mode */
                if (gauge.FlagGAUGE_EN == false || gauge.FlagQEN == false)
                    gauge.CommandSetGaugeEnable();

                await Task.Delay(CmdExecDelayMilliseconds);

                if (gauge.FlagGAUGE_EN == false || gauge.FlagQEN == false)
                {
                    LogView.AddEntry("Failed to enable gauging mode.");
                    return;
                }

                /* Reset to disable resistance update */
                gauge.CommandReset();
                await Task.Delay(ResetCmdExecDelayMilliseconds);

                if (gauge.FlagRDIS == false)
                {
                    LogView.AddEntry("Error: RDIS not set after reset.");
                    return;
                }

                int LStatus_valid = 0;
                if (selectedCycleType == CycleType.ProductionCycle)
                    LStatus_valid = 0x06;
                else if (selectedCycleType == CycleType.LearningCycle)
                    LStatus_valid = 0x04;
                else
                {
                    LogView.AddEntry("Error: selectedCycleType not defined!.");
                    return;
                }

                int LStatus = (int)gauge.GetReadValue("LStatus");
                if (LStatus != LStatus_valid)
                {
                    var mess = @"Error: LStatus != 0x04 " + @"[0x" + LStatus.ToString("X2") + @"].";
                    LogView.AddEntry(mess);
                    return;
                }
            }
            else if (selectedCycleType == CycleType.GpcCycle)
            {
                /* Disable gauging mode */
                if (gauge.FlagGAUGE_EN == true || gauge.FlagQEN == true)
                    gauge.CommandSetGaugeEnable();

                await Task.Delay(CmdExecDelayMilliseconds);

                if (gauge.FlagGAUGE_EN == true || gauge.FlagQEN == true)
                {
                    LogView.AddEntry("Failed to disable gauging mode.");
                    return;
                }
            }
            else
            {
                LogView.AddEntry("Unknown mode.");
                return;
            }

            if (selectedCycleType != CycleType.DischargeChargeTask)
            {
                if (gauge.FlagDSG == false)
                    gauge.CommandToggleDischargeFET();

                /* Set charge and discharge FETs */
                if (gauge.FlagCHG == false)
                    gauge.CommandToggleChargeFET();

                await Task.Delay(CmdExecDelayMilliseconds);

                if (gauge.FlagCHG == false && gauge.FlagDSG == false)
                {
                    LogView.AddEntry("Failed to set charge and discharge FETs.");
                    return;
                }
            }

            LogView.AddEntry("Device preparation successful.");

            int relaxTimeCharge = 0;
            int relaxTimeDischarge = 0;

            float val;
            CfgCycleChargeRelaxHours.Text = CfgCycleChargeRelaxHours.Text.Replace('.', ',');
            if (float.TryParse(CfgCycleChargeRelaxHours.Text, out val))
                relaxTimeCharge = (int)(val * 60); // Convert to minute
            else
                relaxTimeCharge = 120;

            CfgCycleDischargeRelaxHours.Text = CfgCycleDischargeRelaxHours.Text.Replace('.', ',');
            if (float.TryParse(CfgCycleDischargeRelaxHours.Text, out val))
                relaxTimeDischarge = (int)(val * 60); // Conver to minutes
            else
                relaxTimeDischarge = 300;

            Mark5010.IP = IP_Load.Text;

            Properties.Settings.Default.CellCount = CfgCycleCellCount.Text;
            Properties.Settings.Default.TaperCurrent = CfgCycleTaperCurr.Text;
            Properties.Settings.Default.TerminationVoltage = CfgCycleTermVolt.Text;
            Properties.Settings.Default.ChargeRelaxHours = CfgCycleChargeRelaxHours.Text;
            Properties.Settings.Default.DischargeRelaxHours = CfgCycleDischargeRelaxHours.Text;
            Properties.Settings.Default.IP_Load = IP_Load.Text;
            Properties.Settings.Default.CycleRepetitions = int.Parse(CycleRepetitions.Text);
            Properties.Settings.Default.Save();

            List<GenericTask> tl = new List<GenericTask>();

            if (selectedCycleType == CycleType.DischargeChargeTask)
            {
                for(int i = 0; i < int.Parse(CycleRepetitions.Text); i++)
                {
                    tl.AddRange( new List<GenericTask> {
                            new DischargeTask(termVoltage, termVoltageCell),
                            new RelaxTask(relaxTimeDischarge, true),
                            new ChargeTask(taperCurrent),
                            new RelaxTask(relaxTimeCharge, true),
                    } );
                }
            }
            else if (selectedCycleType == CycleType.ProductionCycle)
            {
                tl = new List<GenericTask> {
                    new DischargeTask(termVoltage, termVoltageCell),
                    new RelaxTask(relaxTimeDischarge),
                    new ChargeTask(taperCurrent),
                    new RelaxTask(relaxTimeCharge),
                    new DischargeTask(termVoltage, termVoltageCell, -1, 0x0E),
                };
            }
            else if (selectedCycleType == CycleType.LearningCycle)
            {
                tl = new List<GenericTask> {
                    new DischargeTask(termVoltage, termVoltageCell, 4.0),
                    new RelaxTask(relaxTimeDischarge),
                    new ChargeTask(taperCurrent),
                    new RelaxTask(relaxTimeCharge),
                    new DischargeTask(termVoltage, termVoltageCell, 1.1),
                    new RelaxTask(relaxTimeDischarge),
                };
                

                if (true)
                { // Perform field update cycle to reach LStatus 0x0E
                    tl.AddRange(new GenericTask[]{
                        new ChargeTask(taperCurrent),
                        new RelaxTask(relaxTimeCharge),
                        new DischargeTask(termVoltage, termVoltageCell, 1.1),
                        new RelaxTask(relaxTimeDischarge),
                    });
                }
            }
            else if (selectedCycleType == CycleType.GpcCycle)
            {
                tl = new List<GenericTask> {
                    new RelaxTask(3, true),
                    new ChargeTask(taperCurrent),
                    new RelaxTask(relaxTimeCharge, true),
                    new DischargeTask(termVoltage, termVoltageCell, 0.6),
                    new RelaxTask(relaxTimeDischarge, true),
                };

                gpcLog = new GpcDataLog(cellCount);
            }

            cycle = new Cycle(tl, gauge, Mark5010);
            cycle.CycleModeType = selectedCycleModeType;
            cycle.LogWriteEvent += LogView.AddEntry;
            cycle.CycleCompleted += OnCycleCompleted;
            cycle.StartCycle();

            InUpdatePlot = false;
            ctUpdatePlot = new CancellationTokenSource();
            ThreadUpdatePlot = new Thread(UpdatePlot);
            ThreadUpdatePlot.Start(ctUpdatePlot.Token);
            ctDataLog = new CancellationTokenSource();
            ThreadDataLog = new System.Threading.Thread(UpdateDataLog);
            ThreadDataLog.Start(ctDataLog.Token);

            ButtonCycleStart.IsEnabled = false;
            ButtonCycleCancel.IsEnabled = true;
        }

        /// <summary>
        /// Called on entire cycle completed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCycleCompleted(object sender, EventArgs e)
        {
            if (selectedCycleType == CycleType.ProductionCycle)
            {
                if (gauge != null)
                {
                    if (gauge.FlagFET_EN == false)
                    {
                        gauge.CommandToogleFETenable();
                    }
                    Task.Delay(2 * CmdExecDelayMilliseconds).Wait();
                    if (gauge.FlagFET_EN == false)
                    {
                        LogView.AddEntry("Failed to set FET_EN.");
                    }
                }
            }
            // Add trophy here.
            ctUpdatePlot.Cancel();
            ctDataLog.Cancel();
            ctConnectionM5010.Cancel();

            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                ButtonCycleStart.IsEnabled = true;
                ButtonCycleCancel.IsEnabled = false;
            }));
            
            // --
            if (ThreadConnectionM5010 != null)
                ThreadConnectionM5010.Join();
            if (ThreadUpdatePlot != null)
                ThreadUpdatePlot.Join();
            if (ThreadDataLog != null)
                ThreadDataLog.Join();
            // --
            if (DataLog != null)
            {
                LodDeletEvent();
                DataLog.Dispose();
                DataLog = null;
            }
            if (gpcLog != null)
            {
                gpcLog.Dispose();
                gpcLog = null;
            }
        }

        /// <summary>
        /// Cancel running learning or GPC cycle on button click.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private void ButtonCycleCancel_Click(object sender, RoutedEventArgs e)
        {
            ctUpdatePlot.Cancel();
            ctDataLog.Cancel();

            cycle.CancelCycle();
            ButtonCycleStart.IsEnabled = true;
            ButtonCycleCancel.IsEnabled = false;
            // --
            if (DataLog != null)
            {
                LodDeletEvent();
                DataLog.Dispose();
                DataLog = null;
            }
            if (gpcLog != null)
            {
                gpcLog.Dispose();
                gpcLog = null;
            }
        }

        /// <summary>
        /// Set selected cyle mode on button click.
        /// </summary>
        /// <param name="sender">Get manual or automatic mode from button name.</param>
        /// <param name="e">Not used.</param>
        private void ModeSelectButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button b = (System.Windows.Controls.Button)sender;
            if (b.Name == "ManualModeButton")
            {
                selectedCycleModeType = CycleModeType.Manual;
                ConfigHelpImage.Source = GetImageSourceFromResource("ManualMode.png");
            }
            else if (b.Name == "AutomaticModeButton")
            {
                selectedCycleModeType = CycleModeType.Automatic;
                ConfigHelpImage.Source = GetImageSourceFromResource("AutoMode.png");
            }
            else
            {
                selectedCycleModeType = CycleModeType.None;
                return;
            }

            switch (selectedCycleType)
            {
                case CycleType.ProductionCycle:
                    TabItemCycle.Header = "Production Cycle";
                    break;
                case CycleType.DischargeChargeTask:
                    TabItemCycle.Header = "DischargeCharge Cycle";
                    break;
                case CycleType.LearningCycle:
                    TabItemCycle.Header = "Learning Cycle";
                    break;
                case CycleType.GpcCycle:
                    TabItemCycle.Header = "GPC Cycle";
                    break;
                default:
                    TabItemCycle.Header = "selectedCycleType not defined.";
                    return;
            }

            TabItemCycle.IsEnabled = true;
            TabItemConfiguration.IsEnabled = true;
            System.Windows.Controls.TabItem t = (System.Windows.Controls.TabItem)tabControl.Items[1];
            t.IsSelected = true;

        }

        /// <summary>
        /// Set cycle type on button click.
        /// </summary>
        /// <param name="sender">Get learning or GPC cylce type from button name.</param>
        /// <param name="e">Not used.</param>
        private void CycleTypeSelected(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.RadioButton rb = (System.Windows.Controls.RadioButton)sender;

            if (rb.Name == "ModeDischargeChargeCycle")
            {
                selectedCycleType = CycleType.DischargeChargeTask;
            }
            else if (rb.Name == "ModeLearningCycle")
            {
                selectedCycleType = CycleType.LearningCycle;
            }
            else if(rb.Name == "ModeGpcCycle")
            {
                selectedCycleType = CycleType.GpcCycle;
            }
            else if (rb.Name == "ModeProductionCycle")
            {
                selectedCycleType = CycleType.ProductionCycle;
            }
            else
            {
                selectedCycleType = CycleType.None;
            }
        }

        /// <summary>
        /// Reset zoom on all plot axes.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private void ButtonResetZoom_Click(object sender, RoutedEventArgs e)
        {
            PlotView.ResetAllAxes();
        }
        bool StateConnceted = false;
        //private void TaskConnectionM5010(object sender, System.EventArgs e)
        public void TaskConnectionM5010(object _ct)
        {
            var ct = (CancellationToken)_ct;

            try
            {
                do
                {
                    var IP = Mark5010.IP + @" : " + Mark5010.port.ToString();
                    var label = @"Disconect: IP = " + IP;

                    var Connceted = Mark5010.UpdateConnection();
                    if (StateConnceted != Connceted)
                    {
                        if (Connceted)
                            label = @"Connected: IP = " + IP;
                        else
                            label = @"Disconect: IP = " + IP;

                        StateConnceted = Connceted;
                        LogView.AddEntry(label);
                    }

                    Task.Delay(3000, ct).Wait();
                    //Thread.Sleep(5000);
                } while (!ct.IsCancellationRequested);
            }
            catch (Exception)
            {

            }
        }
    }
}
