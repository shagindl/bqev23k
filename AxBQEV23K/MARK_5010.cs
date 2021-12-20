using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using EasyModbus;

namespace AxBQEV23K
{
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
        UnsolicitedPacket = 10
    }
    public class MARK_5010
    {
        ModbusClient modbusClient;
        string EV2300Name = "BQ40Z80";
        byte[] bEV2300Name;
        short EV2300Version = 1;
        short EV2300Revision = 2;
        enum ADDR_MT2M5010
        {
            xMain_GlobalHdl_DevName = 10125,
            RdWrSMBusBlock = 10200 + 1,
            DataSMBusBlock = 10200 + 1 + 38 / 2,
            WrSMBusWord = 10200 + 1 + 38 / 2 + 34 / 2,
            RdSMBusWord = 10200 + 1 + 38 / 2 + 34 / 2 + 1,
        };
        private Mutex DeviceMutex = new Mutex();

        public MARK_5010()
        {
            modbusClient = new ModbusClient("192.168.15.9", 8000);
            modbusClient.ConnectionTimeout = 10000;
            try
            {
                modbusClient.Connect();
                if (modbusClient.Connected)
                {
                    ;
                }
            }
            catch (Exception ex)
            {
                modbusClient.Disconnect();
                Thread.Sleep(3000);
            }
        }
        public void Dispose()
        {
            modbusClient.Disconnect();
            Thread.Sleep(3000);
            DeviceMutex.Dispose();
        }

        public virtual void AboutBox()
        {
        }
        public virtual short BlastPackets(ref string filename) { return -1; }
        public virtual short CheckForError() { return (short)EV23KError.NoError; }
        public virtual short CloseDevice() { return -1; }
        public virtual short CompareSrecWithCfg(ref string filename, ref string bCfgFilename, ref string pCfgFilename) { return -1; }
        public virtual short ConfigureTarget(int nPFRowEraseDelay, int nPFMassEraseDelay, int nPFProgRowDelay, int nDFRowEraseDelay, int nDFMassEraseDelay, int nDFProgRowDelay, int nPFEraseRowSize, int nPFProgRowSize, int nDFEraseRowSize, int nDFProgRowSize, int nRsvd1, int nRsvd2) { return -1; }
        public virtual void Delay(int nTime) { Thread.Sleep(nTime); }
        public virtual void GetAllBoards(int nNumBrdsToGet, ref int nNumBrds, ref string listBrdNames) { }
        public virtual short GetEV2300Name(ref object nameDataBlock, ref short nLen)
        {
            DeviceMutex.WaitOne();

            bEV2300Name = Encoding.ASCII.GetBytes(EV2300Name);
            nameDataBlock = bEV2300Name;
            nLen = (short)bEV2300Name.Length;

            DeviceMutex.ReleaseMutex();

            return (short)EV23KError.NoError;
        }
        public virtual short GetEV2300Version(ref short nVersion, ref short nRevision)
        {
            nVersion = EV2300Version;
            nRevision = EV2300Revision;

            return (short)EV23KError.NoError;
        }
        public virtual void GetFreeBoards(int nNumBrdsToGet, ref int nNumBrds, ref string listBrdNames)
        {
            DeviceMutex.WaitOne();

            nNumBrdsToGet = 1;
            nNumBrds = 1;
            try
            {
                int[] _Name = modbusClient.ReadInputRegisters((int)ADDR_MT2M5010.xMain_GlobalHdl_DevName, 8);
                listBrdNames = ModbusClient.ConvertRegistersToString(_Name, 0, 2 * _Name.Length).Trim('\0');
            }
            catch (Exception ex)
            {
                nNumBrdsToGet = 0;
                nNumBrds = 0;
            }

            DeviceMutex.ReleaseMutex();
        }
        public virtual short GetPacket(int nLen, ref object vPkt, ref int nBytesPut) { return -1; }
        public virtual short GPIOMask(short nMask) { return (short)EV23KError.NoError; }
        public virtual short GPIORead(short nMask, ref short nData) { return -1; }
        public virtual short GPIOWrite(short nMask, short nData) { return (short)EV23KError.NoError; }
        public virtual short HCF(short nAuthCode) { return -1; }
        public virtual short I2CReadBlock(short nWordAddr, ref object dataBlock, ref short nExpectedLen, short nTargetID) { return -1; }
        //public virtual short I2CReadWrite(global::BQ80XRWLib.I2COperation nOperation, short nTargetAddrOrData, short nWordAddr, ref short nData) { return -1; }
        public virtual short I2CWriteBlock(short nWordAddr, object dataBlock, short nLen, short nTargetID) { return -1; }
        public virtual short MassEraseOption(short nAllow) { return -1; }
        public virtual short OpenDevice(ref string devName) { return (short)EV23KError.NoError; }
        public virtual short ProgramFlash(int nDataFlashStartAddr, int nInstrFlashStartAddr, ref object pDataFlashBlock, int nDataFlashSz, int nRsvdDataFlashSz, ref object pInstrFlashBlock, int nInstrFlashSz, short nTargetID) { return -1; }
        public virtual short ProgramFromSrec(ref string filename) { return -1; }
        public virtual short ProgramFromSrecWithCfg(ref string filename, ref string bCfgFilename, ref string pCfgFilename) { return -1; }
        public virtual short ProgramSrec(ref string filename, short nProtocol, int nPlatform) { return -1; }
        public virtual short PutPacket(int nLen, ref object vPkt, ref int nBytesPut) { return -1; }
        public virtual short ReadFlash(int nDataFlashStartAddr, int nInstrFlashStartAddr, ref object pDataFlashBlock, ref int nDataFlashSz, ref object pInstrFlashBlock, ref int nInstrFlashSz, short nTargetID) { return -1; }
        public virtual short ReadI2CBlockNcmdOnSMB(ref object dataBlock, ref short nLen, short nTargetID) { return -1; }
        public virtual short ReadI2CBlockOnSMB(short nSubCmd, ref object dataBlock, ref short nLen, short nTargetID) { return -1; }
        //public virtual short ReadOneWire(global::BQ80XRWLib.OneWireType eType, short nOneWireCmd, ref short nWord) { return -1; }
        public virtual short ReadSMBusBlock(short nSmbCmd, ref object dataBlock, ref short nLen, short nTargetID)
        {
            DeviceMutex.WaitOne();

            int[] data = modbusClient.ReadHoldingRegisters((int)ADDR_MT2M5010.RdWrSMBusBlock, 1);
            nLen = (short)data[0];
            int Len = data[0] / 2 + (data[0] % 2);
            data = modbusClient.ReadHoldingRegisters((int)ADDR_MT2M5010.DataSMBusBlock, Len);
            Deserialized(data, ref dataBlock, nLen);

            DeviceMutex.ReleaseMutex();

            return 0;
        }
        public virtual short ReadSMBusWord(short nSmbCmd, ref short nWord, short nTargetID) {
            byte[] null_dataBlock = { 0 };
            WriteSMBusBlock(nSmbCmd, null_dataBlock, 0, nTargetID);

            DeviceMutex.WaitOne();

            int[] data = modbusClient.ReadHoldingRegisters((int)ADDR_MT2M5010.RdSMBusWord, 1);
            nWord = (short)data[0];

            DeviceMutex.ReleaseMutex();

            return 0;
        }
        public virtual short ReadSrec(ref string filename, short nFileFormat, short nProtocol, int nPlatform) { return -1; }
        public virtual short SDQProgBlockCustom(object dataBlock, short nLen, short nPulseSetupFactor, short nPulseHoldFactor) { return -1; }
        public virtual short SDQReadBlock(ref object dataBlock, ref short nExpectedLen) { return -1; }
        public virtual short SDQWriteBlock(object dataBlock, short nLen, short nTypeBits, short nPulseSetupFactor, short nPulseHoldFactor) { return -1; }
        public virtual short SetEV2300Name(object nameDataBlock, short nLen) { return -1; }
        public virtual short SetTUSBCharacteristic1(short bytCharacteristic1, short bytMask) { return -1; }
        public virtual short WriteI2CBlockOnSMB(short nSubCmd, object dataBlock, short nLen, short nTargetID) { return -1; }
        //public virtual short WriteOneWire(global::BQ80XRWLib.OneWireType eType, short nOneWireCmd, short nWord);
        public virtual short WriteReadI2CBlockOnSMB(short nSubCmd, short nWR_size, short nRD_size, ref object dataBlock, ref short nLen, short nTargetID) { return -1; }
        public virtual short WriteSMBusBlock(short nSmbCmd, object dataBlock, short len, short nTargetID)
        {
            DeviceMutex.WaitOne();

            //nTargetID = 0xB9;
            int[] regs = Serialized(nTargetID, nSmbCmd, len, dataBlock);

            modbusClient.WriteMultipleRegisters((int)ADDR_MT2M5010.RdWrSMBusBlock, regs);

            DeviceMutex.ReleaseMutex();

            return 0;
        }
        public virtual short WriteSMBusCmd(short nSmbCmd, short nTargetID) { return -1; }
        public virtual short WriteSMBusWord(short nSmbCmd, short nWord, short nTargetID) {
            DeviceMutex.WaitOne();

            int[] regs = Serialized(nTargetID, nSmbCmd, 2, BitConverter.GetBytes(nWord));

            modbusClient.WriteMultipleRegisters((int)ADDR_MT2M5010.WrSMBusWord, regs);

            DeviceMutex.ReleaseMutex();

            return 0; 
        }
        protected void AttachInterfaces() { }
        protected void CreateSink() { }
        protected void DetachSink() { }
        protected int[] Serialized(short nTargetID, short nSmbCmd, short len, object dataBlock)
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(len));
            data.Add((byte)nTargetID);
            data.Add((byte)nSmbCmd);
            data.AddRange((byte[])dataBlock);

            return Serialized(data);
        }
        protected int[] Serialized(List<byte> data)
        {
            List<UInt16> data_U16 = new List<UInt16>();
            if (data.Count() % 2 != 0) data.Add(0);
            for (int i = 0; i < data.Count() / sizeof(UInt16); i++)
            {
                data_U16.Add(BitConverter.ToUInt16(data.ToArray(), i * sizeof(UInt16)));
            }

            int[] regs = Array.ConvertAll(data_U16.ToArray(), Convert.ToInt32);

            return regs;
        }
        protected short Deserialized(int[] data, ref object dst, int Len)
        {
            List<byte> data_byte = new List<byte>();

            for (int i = 0, size = Len; i < data.Length; i++, size--)
            {
                data_byte.Add((byte)(data[i] >> 0));
                if (--size == 0) break;
                data_byte.Add((byte)(data[i] >> 8));
            }
            dst = data_byte.ToArray();

            return (short)Len;
        }

    }
}
