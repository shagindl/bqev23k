using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AxBQEV23K
{
    class xIntPtr
    {
        IntPtr pData;
        public IntPtr IPtr { get => pData; }
        GCHandle _Data;

        public xIntPtr(ref byte[] Data)
        {
            _Data = GCHandle.Alloc(Data, GCHandleType.Pinned);
            pData = _Data.AddrOfPinnedObject();
        }
        ~xIntPtr()
        {
            _Data.Free();
        }
    }

class bq80xrw
    {
        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short WriteSMBusWord(short nSmbCmd, short nWord, short nTargetID);

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short ReadSMBusWord(short nSmbCmd, ref short nWord, short nTargetID);

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short WriteSMBusBlock(short nSmbCmd, IntPtr DataBlock, short Len, short nTargetID);

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short ReadSMBusBlock(short nSmbCmd, IntPtr DataBlock, ref short nLen, short nTargetID);

        [DllImport("bq80xrw.dll", EntryPoint = "OpenDeviceA", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short OpenDeviceA(IntPtr DevName);

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short CloseDevice();

        //public static extern void GetAllFreeBoards(int nNumBrdsToGet, ref int nNumBrds, ref string ListBrdNames, int bufferLength);
        //public static extern void GetAllFreeBoards(
        //    int nNumBrdsToGet, ref int nNumBrds,
        //    [MarshalAs(UnmanagedType.LPStr)]StringBuilder ListBrdNames, 
        //    int bufferLength
        //);
        [DllImport("bq80xrw.dll", EntryPoint = "GetAllFreeBoards", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetAllFreeBoards(
            int nNumBrdsToGet, 
            ref int nNumBrds,
            IntPtr ListBrdNames,
            int bufferLength
        );

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetAllBoards(int nNumBrdsToGet, ref int nNumBrds, ref string ListBrdNames);

        //public virtual short PutPacket(int nLen, ref object vPkt, ref int nBytesPut);

        //public virtual short GetPacket(int nLen, ref object vPkt, ref int nBytesPut);

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short WriteSMBusCmd(short nSmbCmd, short nTargetID);

        //public virtual short I2CReadWrite(I2COperation nOperation, short nTargetAddrOrData, short nWordAddr, ref short nData);
        
        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short CheckForError();

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SetPinVoltage(short channel, short state);

        //public virtual short WriteOneWire(OneWireType eType, short nOneWireCmd, short nWord);

        //public virtual short ReadOneWire(OneWireType eType, short nOneWireCmd, ref short nWord);

        //public virtual short WriteReadI2CBlockOnSMB(short nSubCmd, short nWR_size, short nRD_size, ref object DataBlock, ref short nLen, short nTargetID);

        //public virtual short ReadI2CBlockOnSMB(short nSubCmd, ref object DataBlock, ref short nLen, short nTargetID);

        //public virtual short WriteI2CBlockOnSMB(short nSubCmd, object DataBlock, short nLen, short nTargetID);

        //public virtual short ReadI2CBlockNcmdOnSMB(ref object DataBlock, ref short nLen, short nTargetID);

        //public virtual short ProgramFromSrec(ref string Filename);

        //public virtual short ProgramFromSrecWithCfg(ref string Filename, ref string BCfgFilename, ref string PCfgFilename);

        //public virtual short CompareSrecWithCfg(ref string Filename, ref string BCfgFilename, ref string PCfgFilename);

        //public virtual void Delay(int nTime);

        //public virtual short I2CReadBlock(short nWordAddr, ref object DataBlock, ref short nExpectedLen, short nTargetID);

        //public virtual short I2CWriteBlock(short nWordAddr, object DataBlock, short nLen, short nTargetID);

        [DllImport("bq80xrw.dll", EntryPoint = "GetAdapterFWVersion", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short GetAdapterFWVersion(IntPtr nVersion);

        //public virtual short BlastPackets(ref string Filename);

        //public virtual short ProgramFlash(int nDataFlashStartAddr, int nInstrFlashStartAddr, ref object pDataFlashBlock, int nDataFlashSz, int nRsvdDataFlashSz, ref object pInstrFlashBlock, int nInstrFlashSz, short nTargetID);

        //public virtual short ReadFlash(int nDataFlashStartAddr, int nInstrFlashStartAddr, ref object pDataFlashBlock, ref int nDataFlashSz, ref object pInstrFlashBlock, ref int nInstrFlashSz, short nTargetID);

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short GPIOWrite(short nMask, short nData);

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short GPIORead(byte nMask, ref short nData);

        [DllImport("bq80xrw.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short GPIOSetup(IntPtr pSetup, short magic);

        //public virtual short MassEraseOption(short nAllow);

        [DllImport("bq80xrw.dll", EntryPoint = "GetEV2300Name", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern short GetEV2300Name(IntPtr NameDataBlock, ref short nLen);

        //public virtual short SetEV2300Name(object NameDataBlock, short nLen);

        //public virtual short ConfigureTarget(int nPFRowEraseDelay, int nPFMassEraseDelay, int nPFProgRowDelay, int nDFRowEraseDelay, int nDFMassEraseDelay, int nDFProgRowDelay, int nPFEraseRowSize, int nPFProgRowSize, int nDFEraseRowSize, int nDFProgRowSize, int nRsvd1, int nRsvd2);

        //public virtual short HCF(short nAuthCode);

        //public virtual short ProgramSrec(ref string Filename, short nProtocol, int nPlatform);

        //public virtual short SDQReadBlock(ref object DataBlock, ref short nExpectedLen);

        //public virtual short SDQWriteBlock(object DataBlock, short nLen, short nTypeBits, short nPulseSetupFactor, short nPulseHoldFactor);

        //public virtual short SDQProgBlockCustom(object DataBlock, short nLen, short nPulseSetupFactor, short nPulseHoldFactor);

        //public virtual short ReadSrec(ref string Filename, short nFileFormat, short nProtocol, int nPlatform);

        //public virtual short SetTUSBCharacteristic1(short bytCharacteristic1, short bytMask);

        //public virtual short SetOcxCharacteristic1(int nCharacteristic1, int nMask);

        //public virtual void AboutBox();
    }


    public class EV2400 : UserControl
    {
        public EV2400() { }

        //[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        //public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        public short WriteSMBusWord(short nSmbCmd, short nWord, short nTargetID)
        {
            return bq80xrw.WriteSMBusWord(nSmbCmd, nWord, nTargetID);
        }

        public short ReadSMBusWord(short nSmbCmd, ref short nWord, short nTargetID)
        {
            return bq80xrw.ReadSMBusWord(nSmbCmd, ref nWord, nTargetID);
        }

        public short WriteSMBusBlock(short nSmbCmd, object DataBlock, short Len, short nTargetID)
        {
            byte[] DataBlock_Arr = (byte[])DataBlock;

            return bq80xrw.WriteSMBusBlock(nSmbCmd, new xIntPtr(ref DataBlock_Arr).IPtr, Len, nTargetID);
        }

        public short ReadSMBusBlock(short nSmbCmd, ref object DataBlock, ref short nLen, short nTargetID)
        {
            byte[] DataBlock_Arr = new byte[512];

            var sts = bq80xrw.ReadSMBusBlock(nSmbCmd, new xIntPtr(ref DataBlock_Arr).IPtr, ref nLen, nTargetID);

            DataBlock = DataBlock_Arr;

            return sts;
        }

        public short OpenDevice(ref string DevName)
        {
            byte[] DevName_Arr = Encoding.ASCII.GetBytes(DevName);

            return bq80xrw.OpenDeviceA(new xIntPtr(ref DevName_Arr).IPtr);
        }

        public short CloseDevice()
        {
            return bq80xrw.CloseDevice();
        }

        public void GetFreeBoards(int nNumBrdsToGet, ref int nNumBrds, ref string ListBrdNames)
        {
            byte[] ListBrdNames_Arr = new byte[512];

            bq80xrw.GetAllFreeBoards(nNumBrdsToGet, ref nNumBrds, new xIntPtr(ref ListBrdNames_Arr).IPtr, ListBrdNames_Arr.Length / 2);

            ListBrdNames = Encoding.Default.GetString(ListBrdNames_Arr);
        }

        public void GetAllBoards(int nNumBrdsToGet, ref int nNumBrds, ref string ListBrdNames) {
            bq80xrw.GetAllBoards(nNumBrdsToGet, ref nNumBrds, ref ListBrdNames);
        }

        //public virtual short PutPacket(int nLen, ref object vPkt, ref int nBytesPut);

        //public virtual short GetPacket(int nLen, ref object vPkt, ref int nBytesPut);

        public short WriteSMBusCmd(short nSmbCmd, short nTargetID)
        {
            return bq80xrw.WriteSMBusCmd(nSmbCmd, nTargetID);
        }

        //public virtual short I2CReadWrite(I2COperation nOperation, short nTargetAddrOrData, short nWordAddr, ref short nData);

        public short CheckForError()
        {
            return bq80xrw.CheckForError();
        }

        //public virtual short WriteOneWire(OneWireType eType, short nOneWireCmd, short nWord);

        //public virtual short ReadOneWire(OneWireType eType, short nOneWireCmd, ref short nWord);

        //public virtual short WriteReadI2CBlockOnSMB(short nSubCmd, short nWR_size, short nRD_size, ref object DataBlock, ref short nLen, short nTargetID);

        //public virtual short ReadI2CBlockOnSMB(short nSubCmd, ref object DataBlock, ref short nLen, short nTargetID);

        //public virtual short WriteI2CBlockOnSMB(short nSubCmd, object DataBlock, short nLen, short nTargetID);

        //public virtual short ReadI2CBlockNcmdOnSMB(ref object DataBlock, ref short nLen, short nTargetID);

        //public virtual short ProgramFromSrec(ref string Filename);

        //public virtual short ProgramFromSrecWithCfg(ref string Filename, ref string BCfgFilename, ref string PCfgFilename);

        //public virtual short CompareSrecWithCfg(ref string Filename, ref string BCfgFilename, ref string PCfgFilename);

        //public virtual void Delay(int nTime);

        //public virtual short I2CReadBlock(short nWordAddr, ref object DataBlock, ref short nExpectedLen, short nTargetID);

        //public virtual short I2CWriteBlock(short nWordAddr, object DataBlock, short nLen, short nTargetID);

        public short GetEV2300Version(ref short nVersion, ref short nRevision)
        {
            byte[] nVersion_Arr = new byte[512];

            var sts = bq80xrw.GetAdapterFWVersion(new xIntPtr(ref nVersion_Arr).IPtr);

            var sVersion = Encoding.Default.GetString(nVersion_Arr);
            sVersion = (new Regex(@"\.|\0")).Replace(sVersion, "");
            var ArrVersion = Encoding.ASCII.GetBytes(sVersion);

            nVersion = (short) ( (((int)ArrVersion[0]) << 8) | ArrVersion[1]);
            nRevision = ArrVersion[2];

            return sts;
        }

        //public virtual short BlastPackets(ref string Filename);

        //public virtual short ProgramFlash(int nDataFlashStartAddr, int nInstrFlashStartAddr, ref object pDataFlashBlock, int nDataFlashSz, int nRsvdDataFlashSz, ref object pInstrFlashBlock, int nInstrFlashSz, short nTargetID);

        //public virtual short ReadFlash(int nDataFlashStartAddr, int nInstrFlashStartAddr, ref object pDataFlashBlock, ref int nDataFlashSz, ref object pInstrFlashBlock, ref int nInstrFlashSz, short nTargetID);

        public short GPIOWrite(short nMask, short nData)
        {
            return bq80xrw.GPIOWrite(nMask, nData);
        }

        public short GPIORead(short nMask, ref short nData)
        {
            return bq80xrw.GPIORead((byte)nMask, ref nData);
        }

        public short GPIOMask(short nMask)
        {
            byte[] nMask_Arr = { (byte)nMask, (byte)(nMask >> 8) };

            return bq80xrw.GPIOSetup(new xIntPtr(ref nMask_Arr).IPtr, 1);
        }
        public short GPIOSetup(short nMask)
        {
            byte[] nMask_Arr = { (byte)nMask, (byte)(nMask >> 8) };

            return bq80xrw.GPIOSetup(new xIntPtr(ref nMask_Arr).IPtr, 1);
        }
        public short SetPinVoltage(short channel, short state)
        {
            return bq80xrw.SetPinVoltage(channel, state);
        }
        //public virtual short MassEraseOption(short nAllow);

        public short GetEV2300Name(ref object NameDataBlock, ref short nLen)
        {
            byte[] NameDataBlock_Arr = new byte[512];
            
            var sts = bq80xrw.GetEV2300Name(new xIntPtr(ref NameDataBlock_Arr).IPtr, ref nLen);
            
            NameDataBlock = NameDataBlock_Arr;
            string str = Encoding.Default.GetString(NameDataBlock_Arr);

            return sts;
        }

        //public virtual short SetEV2300Name(object NameDataBlock, short nLen);

        //public virtual short ConfigureTarget(int nPFRowEraseDelay, int nPFMassEraseDelay, int nPFProgRowDelay, int nDFRowEraseDelay, int nDFMassEraseDelay, int nDFProgRowDelay, int nPFEraseRowSize, int nPFProgRowSize, int nDFEraseRowSize, int nDFProgRowSize, int nRsvd1, int nRsvd2);

        //public virtual short HCF(short nAuthCode);

        //public virtual short ProgramSrec(ref string Filename, short nProtocol, int nPlatform);

        //public virtual short SDQReadBlock(ref object DataBlock, ref short nExpectedLen);

        //public virtual short SDQWriteBlock(object DataBlock, short nLen, short nTypeBits, short nPulseSetupFactor, short nPulseHoldFactor);

        //public virtual short SDQProgBlockCustom(object DataBlock, short nLen, short nPulseSetupFactor, short nPulseHoldFactor);

        //public virtual short ReadSrec(ref string Filename, short nFileFormat, short nProtocol, int nPlatform);

        //public virtual short SetTUSBCharacteristic1(short bytCharacteristic1, short bytMask);

        //public virtual short SetOcxCharacteristic1(int nCharacteristic1, int nMask);

        //public virtual void AboutBox();
    }
}
