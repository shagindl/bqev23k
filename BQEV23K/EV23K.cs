using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;

namespace BQEV23K
{
    /// <summary>
    /// EV2300 error codes.
    /// </summary>
    /// <remarks>
    /// Defined in BQ80XRW.dll.
    /// </remarks>
    public enum EV23KError : short
    {
        NoError = 0,
        LostSync = 1,
        NoUSB = 2,
        BadPEC = 3,
        WronNumBytes = 5,
        Unknown = 6,
        SMBCLocked = 260,
        SMBDLocked = 516,
        SMBNAC = 772,
        SMBDLow = 1028,
        SMBLocked = 1284,
        IncorrectParameter = 7, // Invalid parameter type passed to function
        USBTimeoutError = 8,
        InvalidData = 9, // AssemblePacket could not build a valid packet
        UnsolicitedPacket = 10,

        ErrorForeTrie = 0x1000,
        Unknow = -1,
    }

    /// <summary>
    /// EV2300 GPIO mask.
    /// </summary>
    /// <remarks>
    /// Some GPIOs are only accessible on the EV2300 PCB.
    /// </remarks>
    public enum EV23KGpioMask : short
    {
        D19 = 0x0001,       // D17 PCB bottom = LED D19 PCB top, open collector
        D15 = 0x0002,       // D15 PCB bottom, open collector
        D14 = 0x0004,       // D14 PCB bottom, open collector
        D13 = 0x0008,       // D13 PCB bottom, open collector
        VOUT = 0x0010,      // Pin VOUT on HDQ header, push-pull
        HDQ = 0x0020,       // Pin HDQ on HDQ header, push-pull
        I2CSCL = 0x0040,    // I2C SCL pin, open collector
        I2CSDA = 0x0080,    // I2C SDA pin, open collector
        // --
        VOUTunknow = 0x010F,  // ...
        mskVOUTx = 0x0100,  // ...
        VOUT1 = 0x0101,     // PORT1
        VOUT2 = 0x0102,     // PORT2
        VOUT3 = 0x0103,     // PORT3
        VOUT4 = 0x0104,     // PORT4
        // --
        VOUT_LOAD = VOUT1,
        VOUT_CHARG = VOUT2,
        VOUT_START_BTN = VOUT4
    }

    /// <summary>
    /// EV2300 hardware interface control class.
    /// </summary>
    public class EV23K : IDisposable
    {
        //private AxBQ80XRWLib.AxBq80xRW EV23KBoard;
        private AxBQEV23K.EV2400 EV23KBoard;
        private const double CheckStatusPeriodeMilliseconds = 5000;
        private Timer timerCheckStatus;
        private bool isPresent = false;
        private string name = string.Empty;
        private string version = string.Empty;
        private System.Threading.Mutex EV23KMutex = new System.Threading.Mutex();

        public delegate void LogWriteDelegate(object sender, LogWriteEventArgs e);
        public event LogWriteDelegate LogWriteEvent;

        #region Properties
        /// <summary>
        /// Get presents status of EV2300 hardware interface.
        /// </summary>
        public bool IsPresent
        {
            get
            {
                return isPresent;
            }
        }

        /// <summary>
        /// Get name of EV2300 hardware interface.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Get EV2300 version string.
        /// </summary>
        public string Version
        {
            get
            {
                return version;
            }
        }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">Host for BQ80xRW COM object.</param>
        public EV23K(out System.Windows.Forms.Integration.WindowsFormsHost host)
        {
            host = new System.Windows.Forms.Integration.WindowsFormsHost();
            EV23KBoard = new AxBQEV23K.EV2400();
            host.Child = EV23KBoard;

            timerCheckStatus = new Timer(5000);
            timerCheckStatus.Elapsed += new ElapsedEventHandler(CheckStatus);
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
            isPresent = false;
            timerCheckStatus.Close();
            EV23KBoard.Dispose();
            EV23KMutex.Dispose();
        }

        private void LogWrite(string log)
        {
            LogWriteEvent?.Invoke(this, new LogWriteEventArgs(log));
        }

        /// <summary>
        /// Check present status of EV2300 board.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void CheckStatus(object sender, System.EventArgs e)
        {
            try
            {
                EV23KError err = (EV23KError)EV23KBoard.GPIOMask(0);
                if (err == EV23KError.NoUSB)
                {
                    isPresent = false;
                }
                else
                {
                    isPresent = true;
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Connect to EV2300 board.
        /// </summary>
        /// <param name="BrdsNumber">Board numbers found and connected to.</param>
        /// <param name="BrdsName">Connected board name.</param>
        /// <returns>EV2300 error code</returns>
        public EV23KError Connect(out int BrdsNumber, out string BrdsName)
        {
            try
            {
                BrdsNumber = 0;
                BrdsName = "";

                object obj = null;
                short len = 0, ver = 0, rev = 0;

                EV23KBoard.GetFreeBoards(1, ref BrdsNumber, ref BrdsName);

                if (BrdsNumber <= 0)
                    return EV23KError.IncorrectParameter;

                BrdsName = BrdsName.Substring(0, BrdsName.Length - 1);
                EV23KError err = (EV23KError)EV23KBoard.OpenDevice(ref BrdsName);
                if(err == EV23KError.NoError)
                {
                    timerCheckStatus.Enabled = true;
                    CheckStatus(null, null);

                    err = (EV23KError)EV23KBoard.GetEV2300Name(ref obj, ref len);
                    if(name != null)
                    {
                        name = Encoding.ASCII.GetString((byte[])obj);
                        int i = name.IndexOf('\0');
                        if (i >= 0) name = name.Substring(0, i);
                    }

                    err = (EV23KError)EV23KBoard.GetEV2300Version(ref ver, ref rev);
                    if(err == EV23KError.NoError)
                    {
                        var Version = new byte[] { (byte)((ver & 0xff00) >> 8), 0x2E, (byte)(ver & 0x00ff), (byte)rev };
                        version = Encoding.ASCII.GetString(Version);
                    }

                    err = (EV23KError)EV23KBoard.GPIOWrite(0x7FF, 0);
                    err = (EV23KError)GpioLow(EV23KGpioMask.VOUT1);
                    err = (EV23KError)GpioLow(EV23KGpioMask.VOUT2);
                    err = (EV23KError)GpioLow(EV23KGpioMask.VOUT4);
                    // -- Debug
                    //err = (EV23KError)GpioHigh(EV23KGpioMask.VOUT_START_BTN);
                    //err = (EV23KError)GpioLow(EV23KGpioMask.VOUT_START_BTN);

                    //err = (EV23KError)GpioHigh(EV23KGpioMask.VOUT_CHARG);
                    //err = (EV23KError)GpioLow(EV23KGpioMask.VOUT_CHARG);

                    //err = (EV23KError)GpioHigh(EV23KGpioMask.VOUT_LOAD);
                    //err = (EV23KError)GpioLow(EV23KGpioMask.VOUT_LOAD);

                    //err = (EV23KError)GpioToggle(EV23KGpioMask.VOUT3);
                    //err = (EV23KError)GpioToggle(EV23KGpioMask.VOUT3);

                    //err = (EV23KError)GpioToggle(EV23KGpioMask.VOUT4);
                    //err = (EV23KError)GpioToggle(EV23KGpioMask.VOUT4);
                }
                return err;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        /// <summary>
        /// Disconnect EV2300 board.
        /// </summary>
        public void Disconnect()
        {
            isPresent = false;
            timerCheckStatus.Enabled = false;

            Task.Delay(1000);

            try {
                EV23KBoard.CloseDevice();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        public void DisableAll()
        {
            GpioLow(EV23KGpioMask.VOUT1);
            GpioLow(EV23KGpioMask.VOUT2);
            GpioLow(EV23KGpioMask.VOUT4);
        }

        /// <summary>
        /// Read word from device via SMBus on EV2300.
        /// </summary>
        /// <param name="SMBusCmd">Device command to read.</param>
        /// <param name="SMBusWord">Read back device word.</param>
        /// <param name="targetAddr">Device target address.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError ReadSMBusWord(short SMBusCmd, out short SMBusWord, short targetAddr)
        {
            try
            {
                SMBusWord = 0;
                short nWord = 0;
                int trie = 5;
                EV23KError err = EV23KError.NoError;

                if (!isPresent)
                    return EV23KError.NoUSB;

                EV23KMutex.WaitOne();
                while (trie != 0)
                {
                    err = (EV23KError)EV23KBoard.ReadSMBusWord(SMBusCmd, ref nWord, targetAddr);
                    if ((err == EV23KError.SMBNAC || err == EV23KError.UnsolicitedPacket || err == EV23KError.USBTimeoutError) && --trie != 0)
                        continue;
                    else break;
                }
                EV23KMutex.ReleaseMutex();

                if (err != EV23KError.NoError)
                    return err;

                SMBusWord = nWord;
                return EV23KError.NoError;
            }
            catch (Exception ex)
            {
                EV23KMutex.ReleaseMutex();
                throw new ArgumentException(ex.Message);
            }
        }

        /// <summary>
        /// Write command to device via SMBus on EV2300.
        /// </summary>
        /// <param name="SMBusCmd">Device command to write.</param>
        /// <param name="targetAddr">Device target address.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError WriteSMBusCommand(short SMBusCmd, short targetAddr)
        {
            try
            {
                if (!isPresent)
                    return EV23KError.NoUSB;

                EV23KMutex.WaitOne();
                EV23KError err = (EV23KError)EV23KBoard.WriteSMBusCmd(SMBusCmd, (short)(targetAddr - 1));
                EV23KMutex.ReleaseMutex();

                return err;
            }
            catch (Exception ex)
            {
                EV23KMutex.ReleaseMutex();
                throw new ArgumentException(ex.Message);
            }
        }

        /// <summary>
        /// Write word to device via SMBus on EV2300.
        /// </summary>
        /// <param name="SMBusCmd">Device command to write.</param>
        /// <param name="SMBusWord">Data word to write.</param>
        /// <param name="targetAddr">Device target address.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError WriteSMBusWord(short SMBusCmd, short SMBusWord, short targetAddr)
        {
            try
            {
                if (!isPresent)
                    return EV23KError.NoUSB;

                EV23KMutex.WaitOne();
                EV23KError err = (EV23KError)EV23KBoard.WriteSMBusWord(SMBusCmd, SMBusWord, (short)(targetAddr - 1));
                EV23KMutex.ReleaseMutex();

                return err;
            }
            catch (Exception ex)
            {
                EV23KMutex.ReleaseMutex();
                throw new ArgumentException(ex.Message);
            }
        }

        /// <summary>
        /// Read data block from device via SMBus on EV2300.
        /// </summary>
        /// <param name="SMBusCmd">Device command to write.</param>
        /// <param name="DataBlock">Variable to store data block in.</param>
        /// <param name="BlockLength">Read length of data block</param>
        /// <param name="targetAddr">Device target address.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError ReadSMBusBlock(short SMBusCmd, out object DataBlock, out short BlockLength, short targetAddr)
        {
            try
            {
                DataBlock = null;
                BlockLength = 0;
                short len = 0;
                object data = null;
                int trie = 5;
                EV23KError err = EV23KError.NoError;

                if (!isPresent)
                    return EV23KError.NoUSB;

                EV23KMutex.WaitOne();
                while (trie != 0)
                {
                    err = (EV23KError)EV23KBoard.ReadSMBusBlock(SMBusCmd, ref data, ref len, targetAddr);
                    if ((err == EV23KError.SMBNAC || err == EV23KError.UnsolicitedPacket || err == EV23KError.USBTimeoutError) && --trie != 0)
                        continue;
                    else break;
                }
                EV23KMutex.ReleaseMutex();

                if (err != EV23KError.NoError)
                    return err;

                DataBlock = data;
                BlockLength = len;

                return EV23KError.NoError;
            }
            catch (Exception ex)
            {
                EV23KMutex.ReleaseMutex();
                throw new ArgumentException(ex.Message);
            }
        }

        /// <summary>
        /// Write data block to device via SMBus on EV2300.
        /// </summary>
        /// <param name="SMBusCmd">Device command to write.</param>
        /// <param name="DataBlock">Data block to write.</param>
        /// <param name="BlockLength">Write length of data block</param>
        /// <param name="targetAddr">Device target address.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError WriteSMBusBlock(short SMBusCmd, object DataBlock, short BlockLength, short targetAddr)
        {
            try
            {
                if (!isPresent)
                    return EV23KError.NoUSB;

                EV23KMutex.WaitOne();
                EV23KError err = (EV23KError)EV23KBoard.WriteSMBusBlock(SMBusCmd, DataBlock, BlockLength, (short)(targetAddr - 1));
                EV23KMutex.ReleaseMutex();

                return err;
            }
            catch (Exception ex)
            {
                EV23KMutex.ReleaseMutex();
                throw new ArgumentException(ex.Message);
            }
        }

        /// <summary>
        /// Read manufacturer access block from device via SMBus on EV2300.
        /// </summary>
        /// <param name="MacAddr">Manufacturer Access address/command.</param>
        /// <param name="Cmd">Manufacturer Access command/register to read.</param>
        /// <param name="DataBlock">Variable to store data block in.</param>
        /// <param name="BlockLength">Read length of data block</param>
        /// <param name="targetAddr">Device target address.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError ReadManufacturerAccessBlock(short MacAddr, short Cmd, out object DataBlock, out short BlockLength, short targetAddr)
        {
            try
            {
                DataBlock = null;
                BlockLength = 0;
                short len = 0;
                object data = null;
                int trie = 2*5;
                EV23KError err = EV23KError.NoError;

                if (!isPresent)
                    return EV23KError.NoUSB;

                EV23KMutex.WaitOne();
                while (trie != 0)
                {
                    byte[] cmd = { (byte)(Cmd & 0xFF), (byte)((Cmd & 0xFF00) >> 8) };
                    err = (EV23KError)EV23KBoard.WriteSMBusBlock(MacAddr, cmd, (short)2, (short)(targetAddr - 1));

                    if ( (err == EV23KError.SMBNAC || err == EV23KError.UnsolicitedPacket || err == EV23KError.USBTimeoutError) && --trie != 0) 
                        continue;
                    if (err != EV23KError.NoError)
                        goto __error;

                    err = (EV23KError)EV23KBoard.ReadSMBusBlock(MacAddr, ref data, ref len, targetAddr);
                    if ((err == EV23KError.SMBNAC || err == EV23KError.UnsolicitedPacket || err == EV23KError.USBTimeoutError) && --trie != 0)
                        continue;
                    if (err != EV23KError.NoError) goto __error;
                    else break;
                }
                EV23KMutex.ReleaseMutex();

                DataBlock = data;
                BlockLength = len;

                return EV23KError.NoError;
            __error:
                EV23KMutex.ReleaseMutex();
                return err;
            }
            catch (Exception ex)
            {
                EV23KMutex.ReleaseMutex();
                throw new ArgumentException(ex.Message);
            }
        }

        /// <summary>
        /// Read entire dataflash content into buffer.
        /// </summary>
        /// <param name="MacAddr">Manufacturer access command.</param>
        /// <param name="DataBlock">Flash data block.</param>
        /// <param name="BlockLength">Flash data block length.</param>
        /// <param name="targetAddr">Target device address.</param>
        /// <returns>EV2300 error code.</returns>
        /// <remarks>For dataflash access see SLUUBT5, chapter 18.1.75</remarks>
        public EV23KError ReadDataflash(short MacAddr, out object DataBlock, out int BlockLength, short targetAddr)
        {
            try
            {
                DataBlock = null;
                BlockLength = 0;
                short datalen = 32;
                object data = null;
                IEnumerable<byte> df = null;

                if (!isPresent)
                    return EV23KError.NoUSB;

                EV23KMutex.WaitOne();

                byte[] cmd = { 0x00, 0x40 };
                EV23KError err = (EV23KError)EV23KBoard.WriteSMBusBlock(MacAddr, cmd, (short)2, (short)(targetAddr - 1));

                if (err != EV23KError.NoError)
                    goto __error;

                err = (EV23KError)EV23KBoard.ReadSMBusBlock(MacAddr, ref data, ref datalen, targetAddr);
                if (err == EV23KError.NoError)
                {
                    df = ((byte[])data).Skip(2).Take(datalen - 2); // Skip first two address bytes, copy only flash data
                }

                for (int i = 1; i < 105; i++)
                {
                    data = null;
                    datalen = 0;
                    err = (EV23KError)EV23KBoard.ReadSMBusBlock(MacAddr, ref data, ref datalen, targetAddr);
                    if (err == EV23KError.NoError)
                    {
                        df = df.Concat(((byte[])data).Skip(2).Take(datalen - 2)); // Skip frst two address bytes, concat only flash data
                    }
                    else
                        goto __error;
                }

                EV23KMutex.ReleaseMutex();

                DataBlock = df.ToArray();
                BlockLength = df.Count();

                return EV23KError.NoError;
            __error:
                EV23KMutex.ReleaseMutex();
                return err;
            }
            catch (Exception ex)
            {
                EV23KMutex.ReleaseMutex();
                throw new ArgumentException(ex.Message);
            }
        }

        /// <summary>
        /// Get last stored EV2300 error code.
        /// </summary>
        /// <returns>EV2300 error code.</returns>
        public EV23KError CheckForError()
        {
            if (!isPresent)
                return EV23KError.NoUSB;

            return (EV23KError)EV23KBoard.CheckForError();
        }
        EV23KGpioMask state_GPIO = EV23KGpioMask.VOUTunknow;
        /// <summary>
        /// Set EV2300 GPIO pin high.
        /// </summary>
        /// <param name="gpio">GPIO pin mask to set.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError GpioHigh(EV23KGpioMask gpio)
        {
            EV23KError err = EV23KError.NoError, err2log = EV23KError.Unknow;

            if (!isPresent)
                return EV23KError.NoUSB;

            EV23KMutex.WaitOne();
            switch (gpio) {
                case EV23KGpioMask.VOUT1:
                case EV23KGpioMask.VOUT2:
                case EV23KGpioMask.VOUT4:
                    if (state_GPIO != EV23KGpioMask.VOUTunknow)
                    {
                        var state = ((state_GPIO & gpio) == gpio) ? true : false;
                        if (state)
                            goto __exit;
                    }
                    else
                        state_GPIO = EV23KGpioMask.mskVOUTx;

                    state_GPIO |= (gpio & ~EV23KGpioMask.mskVOUTx);
                    var indx_gpio = (short)(gpio & ~EV23KGpioMask.mskVOUTx);
                    err = err2log  = (EV23KError)EV23KBoard.SetPinVoltage(indx_gpio, 1);
                    goto __exit;
            }
            err = err2log = (EV23KError)EV23KBoard.GPIOWrite((short)gpio, (short)gpio);

        __exit:
            EV23KMutex.ReleaseMutex();
            if (err2log != EV23KError.Unknow) LogWrite($"GpioHigh {gpio} err = {err}");

            return err;
        }

        /// <summary>
        /// Set EV2300 GPIO pin low.
        /// </summary>
        /// <param name="gpio">GPIO pin mask to set.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError GpioLow(EV23KGpioMask gpio)
        {
            EV23KError err = EV23KError.NoError, err2log = EV23KError.Unknow;

            if (!isPresent)
                return EV23KError.NoUSB;

            EV23KMutex.WaitOne();
            switch (gpio)
            {
                case EV23KGpioMask.VOUT1:
                case EV23KGpioMask.VOUT2:
                case EV23KGpioMask.VOUT4:
                    if (state_GPIO != EV23KGpioMask.VOUTunknow)
                    {
                        var state = ((state_GPIO & gpio) == gpio) ? true : false;
                        if (!state)
                            goto __exit;
                    }
                    else
                        state_GPIO = EV23KGpioMask.mskVOUTx;

                    state_GPIO &= ~(gpio & ~EV23KGpioMask.mskVOUTx);
                    var indx_gpio = (short)(gpio & ~EV23KGpioMask.mskVOUTx);
                    err = err2log = (EV23KError)EV23KBoard.SetPinVoltage(indx_gpio, 0);
                    goto __exit;
            }

            err = err2log = (EV23KError)EV23KBoard.GPIOWrite((short)gpio, 0);

        __exit:
            EV23KMutex.ReleaseMutex();
            if(err2log != EV23KError.Unknow) LogWrite($"GpioLow {gpio} err = {err}");
            return err;
        }

        /// <summary>
        /// Toogle EV2300 GPIO pin.
        /// </summary>
        /// <param name="gpio">GPIO pin mask to toggle.</param>
        /// <returns>EV2300 error code.</returns>
        public EV23KError GpioToggle(EV23KGpioMask gpio)
        {
            EV23KError err = EV23KError.NoError, err2log = EV23KError.Unknow;
            int state = -1;

            if (!isPresent)
                return EV23KError.NoUSB;

            EV23KMutex.WaitOne();
            switch (gpio)
            {
                case EV23KGpioMask.VOUT1:
                case EV23KGpioMask.VOUT2:
                case EV23KGpioMask.VOUT4:
                    state = ((state_GPIO & gpio) == gpio) ? 0 : 1;
                    state_GPIO = state == 1 ? (state_GPIO | gpio) : (state_GPIO & ~(gpio & ~EV23KGpioMask.mskVOUTx));
                    var indx_gpio = (short)(gpio & ~EV23KGpioMask.mskVOUTx);
                    err = err2log = (EV23KError)EV23KBoard.SetPinVoltage(indx_gpio, (short)state);

                    goto __exit;
            }

            short data = 0;
            err = (EV23KError)EV23KBoard.GPIORead((short)gpio, ref data);
            if(err == EV23KError.NoError)
            {
                data = (short)~data;
                err = err2log =(EV23KError)EV23KBoard.GPIOWrite((short)gpio, data);
            }
            state = data;

        __exit:
            EV23KMutex.ReleaseMutex();
            if (err2log != EV23KError.Unknow) LogWrite($"GpioToggle {gpio} state = {state} err = {err}");

            return err;
        }
    }
}
