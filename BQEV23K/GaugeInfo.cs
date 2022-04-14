using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BQEV23K
{
    /// <summary>
    /// Gauge/device handling class. Reads device configuration, register data and issues commands.
    /// </summary>
    public class GaugeInfo : IDisposable
    {
        private const int GaugeDataPollingInterval = 100;
        private readonly List<string> singleReadGaugeDataRegisters = new List<string> {
            "Device Chemistry",
            "Manufacturer Name",
            // --
            //"Absolute State of Charge",
            //"Run time To Empty",
            //"Average Time to Empty",
            //"Average Time to Full",
            //"Charging Current",
            //"Charging Voltage",

            //"QMax Passed Q","QMax Time",
            //"DOD0 Passed Q","DOD0 Passed E","DOD0 Time",
            //"Cell 1 QMax","Cell 2 QMax","Cell 3 QMax","Cell 4 QMax",
            //"Cell 1 QMax DOD0","Cell 2 QMax DOD0","Cell 3 QMax DOD0","Cell 4 QMax DOD0",
            //"Cell 1 Raw DOD","Cell 2 Raw DOD","Cell 3 Raw DOD","Cell 4 Raw DOD",
            //"Cell 1 DOD0","Cell 2 DOD0","Cell 3 DOD0","Cell 4 DOD0",
            //"Cell 1 DODEOC","Cell 2 DODEOC","Cell 3 DODEOC","Cell 4 DODEOC",
            //"Cell 1 Grid","Cell 2 Grid","Cell 3 Grid","Cell 4 Grid",
        };
        

        private string[] cyclicReadGaugeDataRegisters = new string[]{
            "Voltage",
            "Temperature",
            "Current",
            "LStatus",
            "IT Status",
            "Battery Status",
            "Manufacturing Status",
            "Operation Status A",
            "Operation Status B",
            "Relative State of Charge",
            //"Absolute State of Charge",

            "Charging Status","Gauging Status",
            "Safety Status A+B", "Safety Status C+D",
            "Safety Alert A+B","Safety Alert C+D",
            "PF Status A+B","PF Status C+D",
            "PF Alert A+B","PF Alert C+D",

            //"Run time To Empty",
            "Average Time to Empty",
            "Average Time to Full",
            //"Charging Current",
            //"Charging Voltage",

            "QMax Passed Q",/*"QMax Time",*/
            "DOD0 Passed Q","DOD0 Passed E",/*"DOD0 Time",*/
            "Cell 1 QMax",/*"Cell 2 QMax","Cell 3 QMax","Cell 4 QMax",*/
            "Cell 1 QMax DOD0", /*"Cell 2 QMax DOD0","Cell 3 QMax DOD0","Cell 4 QMax DOD0",*/
            "Cell 1 Raw DOD", /*"Cell 2 Raw DOD","Cell 3 Raw DOD","Cell 4 Raw DOD",*/
            "Cell 1 DOD0",/*"Cell 2 DOD0","Cell 3 DOD0","Cell 4 DOD0",*/
            "Cell 1 DODEOC",/*"Cell 2 DODEOC","Cell 3 DODEOC","Cell 4 DODEOC",*/
            "Cell 1 Grid",/*"Cell 2 Grid","Cell 3 Grid","Cell 4 Grid",*/

            "Cell 1 Voltage",
            "Cell 2 Voltage",
            "Cell 3 Voltage",
            "Cell 4 Voltage",
        };

        private EV23K EV23KBoard;
        private SbsItems sbsItems;
        private BcfgItems bcfgItems;
        private CancellationTokenSource cancelSource;
        private int voltage = 0, voltage_Cell1 = 0, voltage_Cell2 = 0, voltage_Cell3 = 0, voltage_Cell4 = 0;
        private int current = 0;
        private double temperature = 0.0;
        Thread ThreadPollTimer;
        private bool hasEV23KError = false;
        private bool hasSMBusError = false;
        private bool isReadingGauge = false;
        private bool isDataflashAvail = false;
        private Mutex readDeviceMutex = new Mutex();

        public delegate void LogWriteDelegate(object sender, LogWriteEventArgs e);
        public event LogWriteDelegate LogWriteEvent;

        #region Properties
        /// <summary>
        /// Get battery voltage.
        /// </summary>
        public int Voltage
        {
            get
            {
                return voltage;
            }
        }
        public int VoltageCell1
        {
            get
            {
                return voltage_Cell1;
            }
        }
        public int VoltageCell2
        {
            get
            {
                return voltage_Cell2;
            }
        }
        public int VoltageCell3
        {
            get
            {
                return voltage_Cell3;
            }
        }
        public int VoltageCell4
        {
            get
            {
                return voltage_Cell4;
            }
        }


        /// <summary>
        /// Get battery current.
        /// </summary>
        public int Current
        {
            get
            {
                return current;
            }
        }

        /// <summary>
        /// Get battery temperature.
        /// </summary>
        public double Temperature
        {
            get
            {
                return temperature;
            }
        }

        /// <summary>
        /// True on EV2300 returned error code.
        /// </summary>
        public bool HasEV23KError
        {
            get
            {
                return hasEV23KError;
            }
        }

        /// <summary>
        /// True on SMBus communication error.
        /// </summary>
        public bool HasSMBusError
        {
            get
            {
                return hasSMBusError;
            }
        }
        /// <summary>
        /// True when async thread is reading the device/gauge register data.
        /// </summary>
        public bool IsReadingGauge
        {
            get
            {
                return isReadingGauge;
            }
        }

        /// <summary>
        /// Get status of VOK flag.
        /// </summary>
        public bool FlagVOK
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "IT Status").SbsBitItems.Find(x => x.SbsCaption == "VOK").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of REST flag.
        /// </summary>
        public bool FlagREST
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "IT Status").SbsBitItems.Find(x => x.SbsCaption == "REST").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of RDIS flag.
        /// </summary>
        public bool FlagRDIS
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "IT Status").SbsBitItems.Find(x => x.SbsCaption == "RDIS").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of QMAX flag.
        /// </summary>
        public bool FlagQMAX
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "IT Status").SbsBitItems.Find(x => x.SbsCaption == "QMAX").SbsBitValue != 0;
            }
        }
        public bool FlagVDQ
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "IT Status").SbsBitItems.Find(x => x.SbsCaption == "VDQ").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of QEN flag.
        /// </summary>
        public bool FlagQEN
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "IT Status").SbsBitItems.Find(x => x.SbsCaption == "QEN").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of FET_EN flag.
        /// </summary>
        public bool FlagOCV
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "IT Status").SbsBitItems.Find(x => x.SbsCaption == "OCVFR").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of FC flag.
        /// </summary>
        public bool FlagFC
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "Battery Status").SbsBitItems.Find(x => x.SbsCaption == "FC").SbsBitValue != 0;
            }
        }
        public bool FlagFD
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "Battery Status").SbsBitItems.Find(x => x.SbsCaption == "FD").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of GAUGE_EN flag.
        /// </summary>
        public bool FlagGAUGE_EN
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "Manufacturing Status").SbsBitItems.Find(x => x.SbsCaption == "GAUGE_EN").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of CHG flag.
        /// </summary>
        public bool FlagCHG
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "Operation Status A").SbsBitItems.Find(x => x.SbsCaption == "CHG").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of DSG flag.
        /// </summary>
        public bool FlagDSG
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "Operation Status A").SbsBitItems.Find(x => x.SbsCaption == "DSG").SbsBitValue != 0;
            }
        }

        /// <summary>
        /// Get status of FET_EN flag.
        /// </summary>
        public bool FlagFET_EN
        {
            get
            {
                return sbsItems.SbsRegister.Find(x => x.Caption == "Manufacturing Status").SbsBitItems.Find(x => x.SbsCaption == "FET_EN").SbsBitValue != 0;
            }
        }
        
        public string DFCellCount
        {
            get
            {
                var item = bcfgItems.DataflashItems.Find(x => x.Caption == "Cell Configuration");
                return bcfgItems.DataflashItems.Find(x => x.Caption == "Cell Configuration").RawValue.ToString();
            }
        }

        public string DFTermVoltage
        {
            get
            {
                return bcfgItems.DataflashItems.Find(x => x.Caption == "Term Voltage").RawValue.ToString();
            }
        }

        public string DFTaperCurrent
        {
            get
            {
                return bcfgItems.DataflashItems.Find(x => x.Caption == "Charge Term Taper Current").RawValue.ToString();
            }
        }

        public string DFDsgCurrentThreshold
        {
            get
            {
                return bcfgItems.DataflashItems.Find(x => x.Caption == "Dsg Current Threshold").RawValue.ToString();
            }
        }

        public string DFChgCurrentThreshold
        {
            get
            {
                return bcfgItems.DataflashItems.Find(x => x.Caption == "Chg Current Threshold").RawValue.ToString();
            }
        }

        public string DFDesignVoltage
        {
            get
            {
                return bcfgItems.DataflashItems.Find(x => x.Caption == "Design Voltage").RawValue.ToString();
            }
        }

        public string DFDesignCapacity
        {
            get
            {
                return bcfgItems.DataflashItems.Find(x => x.Caption == "Design Capacity mAh").RawValue.ToString();
            }
        }

        public Mutex ReadDeviceMutex
        {
            get
            {
                return readDeviceMutex;
            }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ev">Reference to EV2300 board</param>
        public GaugeInfo(EV23K ev)
        {
            EV23KBoard = ev;
            sbsItems = new SbsItems(@"Resources/4800_0_04-bq40z80.bqz");
            //bcfgItems = new BcfgItems(@"Resources/4800_0_04-bq40z80.bcfgx");
            bcfgItems = new BcfgItems(@"Resources/4800_0_04-bq40z80.clipped.bcfgx");
            
            WriteDevice("DEVICE_NUMBER");
            WriteDevice("HW_VERSION");
            WriteDevice("FW_VERSION");
            WriteDevice("FW_BUILD");
            WriteDevice("CHEM_ID");
            ReadDevice("Device Name");

            StartPolling();
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
            cancelSource.Cancel();
            EV23KBoard.Dispose();
        }

        /// <summary>
        /// Start polling register data from device.
        /// </summary>
        public void StartPolling()
        {
            cancelSource = new CancellationTokenSource();
            ThreadPollTimer = new Thread(ReadGaugeData);
            ThreadPollTimer.Start(cancelSource.Token);
        }

        /// <summary>
        /// Stop polling register data from device.
        /// </summary>
        public void StopPolling()
        {
            if(!cancelSource.IsCancellationRequested)
                cancelSource.Cancel();
        }

        /// <summary>
        /// Read data from device registers listed in cyclicReadGaugeDataRegisters.
        /// </summary>
        /// <param name="state">Not used.</param>
        private void ReadGaugeData(object _ct)
        {
            var ct = (CancellationToken)_ct;

            try
            {
                // -- Single read
                foreach (var cmd in singleReadGaugeDataRegisters)
                {
                    if (ReadDevice(cmd) != EV23KError.NoError)
                        hasSMBusError = true;
                }
                // -- Cycle read
                do
                {
                    if (!EV23KBoard.IsPresent)
                    {
                        hasEV23KError = true;
                        return;
                    }

                    hasSMBusError = false;

                    readDeviceMutex.WaitOne();

                    ReadDataflash();
                    foreach (string cmd in cyclicReadGaugeDataRegisters)
                    {
                        if (ReadDevice(cmd) != EV23KError.NoError)
                            hasSMBusError = true;
                    }

                    voltage = (int)GetReadValue("Voltage");
                    voltage_Cell1 = (int)GetReadValue("Cell 1 Voltage");
                    voltage_Cell2 = (int)GetReadValue("Cell 2 Voltage");
                    voltage_Cell3 = (int)GetReadValue("Cell 3 Voltage");
                    voltage_Cell4 = (int)GetReadValue("Cell 4 Voltage");

                    temperature = GetReadValue("Temperature");
                    current = (int)GetReadValue("Current");

                    readDeviceMutex.ReleaseMutex();

                    Task.Delay(GaugeDataPollingInterval, ct).Wait();
                } while (!ct.IsCancellationRequested);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Read specific device register using methode defined in configuration.
        /// </summary>
        /// <param name="caption">Register name to read.</param>
        /// <returns>EV2300 error code.</returns>
        private EV23KError ReadDevice(string caption)
        {
            EV23KError err = EV23KError.Unknown;
            short dataWord = 0;
            object dataBlock = null;
            short dataLength = 0;

            SbsRegisterItem i = sbsItems.SbsRegister.Find(x => x.Caption == caption);
            if(i != null)
            {
                if (i.ReadStyle == 1)
                {
                    err = (EV23KError)EV23KBoard.ReadSMBusWord(i.Command, out dataWord, sbsItems.TargetAdress);
                    if(err == EV23KError.NoError)
                    {
                        i.RawValue = dataWord;
                        i.CalculateBitFields();
                        i.CalculateDisplayValue();
                    }
                    else
                    {

                    }
                }
                else if (i.ReadStyle == 2)
                {
                    if (!i.IsMac)
                    {
                        err = (EV23KError)EV23KBoard.ReadSMBusBlock(i.Command, out dataBlock, out dataLength, sbsItems.TargetAdress);
                        if (err == EV23KError.NoError)
                        {
                            i.GetRawValueFromDataBlock(dataBlock, dataLength, i.OffsetWithinBlock, i.LengthWithinBlock);
                            i.CalculateBitFields();
                            i.CalculateDisplayValue();
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        err = (EV23KError)EV23KBoard.ReadManufacturerAccessBlock(sbsItems.TargetMacAdress, i.Command, out dataBlock, out dataLength, sbsItems.TargetAdress);
                        if (err == EV23KError.NoError)
                        {
                            i.GetRawValueFromDataBlock(dataBlock, dataLength, i.OffsetWithinBlock, i.LengthWithinBlock);
                            i.CalculateBitFields();
                            i.CalculateDisplayValue();
                        }
                        else
                        {

                        }
                    }
                }
                else if (i.ReadStyle == 3)
                {
                    err = (EV23KError)EV23KBoard.ReadManufacturerAccessBlock(sbsItems.TargetMacAdress, i.Command, out dataBlock, out dataLength, sbsItems.TargetAdress);
                    if (err == EV23KError.NoError)
                    {
                        i.GetRawValueFromDataBlock(dataBlock, dataLength, i.OffsetWithinBlock, i.LengthWithinBlock);
                        i.CalculateBitFields();
                        i.CalculateDisplayValue();
                    }
                    else
                    {

                    }
                }
            }

            if (err != EV23KError.NoError)
            {

            }

            return err;
        }

        /// <summary>
        /// Write specific device register using methode defined in configuration.
        /// </summary>
        /// <param name="caption">Register name to write.</param>
        /// <returns>EV2300 error code.</returns>
        /// <remarks>For now only works to set the ManufacturerAccess command.</remarks>
        private EV23KError WriteDevice(string caption)
        {
            EV23KError err = EV23KError.Unknown;
            object dataBlock = null;
            short dataLength = 0;

            SbsCommandItem c = sbsItems.SbsCommands.Find(x => x.Caption == caption);
            if( c != null)
            {
                if(c.WriteStyle == 1)
                {
                    err = EV23KBoard.WriteSMBusWord(0, c.Command, sbsItems.TargetAdress);
                }
                else if(c.WriteStyle == 2)
                {
                    err = EV23KBoard.ReadManufacturerAccessBlock(sbsItems.TargetMacAdress, c.Command, out dataBlock, out dataLength, sbsItems.TargetAdress);
                    if (c.HasResult && err == EV23KError.NoError)
                    {
                        c.GetRawValueFromDataBlock(dataBlock, dataLength, c.OffsetWithinBlock, c.LengthWithinBlock);
                        c.CalculateDisplayValue();
                    }
                }
            }
            LogWrite($"cmd = {caption} err = {err}");

            return err;
        }

        /// <summary>
        /// Read entire data flash from device if not already present.
        /// </summary>
        private  void ReadDataflash()
        {
            if (isDataflashAvail)
                return;

            EV23KError err = EV23KError.Unknown;
            object dataBlock = null;
            int dataLength = 0;

            if(EV23KBoard.IsPresent)
            {
                err = EV23KBoard.ReadDataflash(sbsItems.TargetMacAdress, out dataBlock, out dataLength, sbsItems.TargetAdress);
                if (err == EV23KError.NoError)
                {
                    bcfgItems.CreateDataflashModel(dataBlock, dataLength);
                    isDataflashAvail = true;
                }
            }
            return;
        }
        private void LogWrite(string log)
        {
            LogWriteEvent?.Invoke(this, new LogWriteEventArgs(log));
        }

        /// <summary>
        /// Get formatted string of register value, scaled and including the unit.
        /// </summary>
        /// <param name="caption">Register name to read from.</param>
        /// <returns>Formatted value string.</returns>
        public string GetDisplayValue(string caption)
        {
            SbsRegisterItem i = sbsItems.SbsRegister.Find(x => x.Caption == caption);
            if (i == null)
            {
                SbsCommandItem c = sbsItems.SbsCommands.Find(x => x.Caption == caption);
                if (c == null)
                    return "";
                else
                    return c.DisplayValue.Replace(',', '.');
            }
            else
            {
                return i.DisplayValue.Replace(',', '.');
            }
        }
        public string GetShortName(string caption)
        {
            SbsRegisterItem i = sbsItems.SbsRegister.Find(x => x.Caption == caption);
            if (i != null)
            {
                return i.LogCaption.Replace(',', '.');
            }
            return "???";
        }

        /// <summary>
        /// Get scaled register value.
        /// </summary>
        /// <param name="caption">Register name to read from.</param>
        /// <returns>Scaled value.</returns>
        public double GetReadValue(string caption)
        {
            SbsRegisterItem i = sbsItems.SbsRegister.Find(x => x.Caption == caption);
            if(i == null)
            {
                SbsCommandItem c = sbsItems.SbsCommands.Find(x => x.Caption == caption);
                if (c == null)
                    return 0;
                else
                    return c.ReadValue;
            }
            else
            {
                return i.ReadValue;
            }
        }

        /// <summary>
        /// Send toggle charge FET command to device.
        /// </summary>
        public void CommandToggleChargeFET()
        {
            WriteDevice("CHG_FET_TOGGLE");
        }

        /// <summary>
        /// Send toggle discharge FET command to device.
        /// </summary>
        public void CommandToggleDischargeFET()
        {
            WriteDevice("DSG_FET_TOGGLE");
        }

        /// <summary>
        /// Send FET enable command to device.
        /// </summary>
        public void CommandToogleFETenable()
        {
            WriteDevice("FET_EN");
        }

        /// <summary>
        /// Send gauge enable command to device.
        /// </summary>
        public void CommandSetGaugeEnable()
        {
            WriteDevice("GAUGE_EN");
        }

        /// <summary>
        /// Commands a gauge reset with defined waiting time.
        /// </summary>
        public void CommandReset()
        {
            WriteDevice("RESET");
        }
        
        /// <summary>
        /// Changes status of the charger relay connected to EV2300 on pin VOUT.
        /// </summary>
        /// <param name="state">Logical ouput state.</param>
        public void ToggleChargerRelay(bool state)
        {
            if (state)
                EV23KBoard.GpioHigh(EV23KGpioMask.VOUT_CHARG);
            else
                EV23KBoard.GpioLow(EV23KGpioMask.VOUT_CHARG);
        }

        /// <summary>
        /// Changes status of the load relay connected to EV2300 on pin HDQ.
        /// </summary>
        /// <param name="state">Logical output state.</param>
        public void ToggleLoadRelay(bool state)
        {
            if (state)
                EV23KBoard.GpioHigh(EV23KGpioMask.VOUT_LOAD);
            else
                EV23KBoard.GpioLow(EV23KGpioMask.VOUT_LOAD);
        }

        /// <summary>
        /// Pushes a remote button via relay starting the load bench.
        /// </summary>
        public async void RemoteLoadStartButton()
        {
            await Task.Delay(3000);
            EV23KBoard.GpioHigh(EV23KGpioMask.VOUT_START_BTN);
            await Task.Delay(100);
            EV23KBoard.GpioLow(EV23KGpioMask.VOUT_START_BTN);
        }
    }
}
