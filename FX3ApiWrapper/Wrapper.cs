﻿using System;
using System.Linq;
using FX3Api;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using RegMapClasses;
using adisInterface;

namespace FX3ApiWrapper
{
    /// <summary>
    /// Simplified enum for configuring sensor type
    /// </summary>
    public enum SensorType
    {
        /// <summary>
        /// Standard IMU. This includes the following IMU families:
        /// ADIS1647x
        /// ADIS1649x
        /// ADIS1650x
        /// ADIS1655x
        /// </summary>
        StandardImu = 0,

        /// <summary>
        /// Automotive SPI IMU. This includes the following IMU families:
        /// ADXC1500
        /// </summary>
        AutomotiveSpiImu = 1,

        /// <summary>
        /// Legacy IMU. This includes the following IMU families
        /// ADIS1644x
        /// </summary>
        LegacyImu = 2,

        /// <summary>
        /// Single axis ADcmXL
        /// </summary>
        ADcmXL1021 = 3,

        /// <summary>
        /// Dual axis ADcmXL
        /// </summary>
        ADcmXL2021 = 4,

        /// <summary>
        /// Tri axis ADcmXL
        /// </summary>
        ADcmXL3021 = 5
    }

    /// <summary>
    /// Simplified wrapper around FX3 connection object
    /// </summary>
    [ComVisible(true)]
    public class Wrapper
    {
        /// <summary>
        /// Underlying FX3 connection object
        /// </summary>
        public FX3Connection FX3;

        /// <summary>
        /// Underlying DUT interface object
        /// </summary>
        public IDutInterface Dut;

        /* Private members */
        private RegMapCollection m_RegMap;
        private List<RegClass> m_StreamRegs;
        private uint m_regSize;

        /// <summary>
        /// Wrapper constructor. Loads resources and connects to FX3
        /// </summary>
        /// <param name="FX3ResourcePath">Path the FX3 firmware binaries</param>
        /// <param name="RegMapPath">Path to register map file</param>
        /// <param name="Type"></param>
        public Wrapper(string FX3ResourcePath, string RegMapPath, SensorType Type)
        {
            FX3 = new FX3Connection(FX3ResourcePath, FX3ResourcePath, FX3ResourcePath);
            ConnectToBoard();
            UpdateDutType(Type);
            UpdateRegMap(RegMapPath);
        }

        /// <summary>
        /// Class destructor. Disconnects FX3
        /// </summary>
        ~Wrapper()
        {
            Disconnect();
        }

        /// <summary>
        /// Disconnect FX3 board
        /// </summary>
        public void Disconnect()
        {
            FX3.Disconnect();
        }

        /// <summary>
        /// Update the IDutInterface type based on the current FX3 setting.
        /// For SensorType of ADcmXL will use the appropriate ADcmXL Interface (ADcmXLx021)
        /// For SensorType of AutomotiveSpi will use ZeusInterface (ADIS1655x)
        /// For SensorType of IMU, PartType LegacyIMU will use aducInterface (ADIS1644x)
        /// For SensorType of IMU, PartType IMU, will use adbfInterface (ADIS1647x, ADIS1649x, ADIS1650x)
        /// </summary>
        public void UpdateDutType(SensorType Type)
        {
            m_regSize = 2;
            switch(Type)
            {
                case SensorType.AutomotiveSpiImu:
                    m_regSize = 4;
                    FX3.SensorType = DeviceType.AutomotiveSpi;
                    FX3.PartType = DUTType.IMU;
                    Dut = new ZeusInterface(FX3, null);
                    break;
                case SensorType.LegacyImu:
                    FX3.SensorType = DeviceType.IMU;
                    FX3.PartType = DUTType.LegacyIMU;
                    Dut = new aducInterface(FX3, null);
                    break;
                case SensorType.ADcmXL1021:
                    FX3.SensorType = DeviceType.ADcmXL;
                    FX3.PartType = DUTType.ADcmXL1021;
                    Dut = new AdcmInterface1Axis(FX3, null);
                    break;
                case SensorType.ADcmXL2021:
                    FX3.SensorType = DeviceType.ADcmXL;
                    FX3.PartType = DUTType.ADcmXL2021;
                    Dut = new AdcmInterface2Axis(FX3, null);
                    break;
                case SensorType.ADcmXL3021:
                    FX3.SensorType = DeviceType.ADcmXL;
                    FX3.PartType = DUTType.ADcmXL3021;
                    Dut = new AdcmInterface3Axis(FX3, null);
                    break;
                case SensorType.StandardImu:
                default:
                    FX3.SensorType = DeviceType.IMU;
                    FX3.PartType = DUTType.IMU;
                    Dut = new adbfInterface(FX3, null);
                    break;
            }
        }

        /// <summary>
        /// Reload the regmap based on provided CSV path
        /// </summary>
        /// <param name="RegMapPath">Path to RegMap CSV file</param>
        public void UpdateRegMap(string RegMapPath)
        {
            m_RegMap = new RegMapCollection();
            m_RegMap.ReadFromCSV(RegMapPath);
        }

        /// <summary>
        /// Get or set number of bytes per register. Used when directly addressing registers (not using RegMap)
        /// </summary>
        public uint RegNumBytes
        {
            get
            {
                return m_regSize;
            }
            set
            {
                m_regSize = value;
            }
        }

        #region "Register Read"

        /// <summary>
        /// Read single unsigned register
        /// </summary>
        /// <param name="RegName">Name of register to read. Must be in RegMap</param>
        /// <returns>Unsigned register value</returns>
        public uint ReadUnsigned(string RegName)
        {
            return Dut.ReadUnsigned(m_RegMap[RegName]);
        }

        /// <summary>
        /// Read multiple unsigned registers
        /// </summary>
        /// <param name="RegNames">Names of all registers to read</param>
        /// <returns>Array of register read values</returns>
        public uint[] ReadUnsigned(string[] RegNames)
        {
            return ReadUnsigned(RegNames, 1, 1);
        }

        /// <summary>
        /// Read set of multiple unsigned registers, numCaptures times
        /// </summary>
        /// <param name="RegNames">Names of all registers to read</param>
        /// <param name="NumCaptures">Number of times to read the register list</param>
        /// <returns>Array of register read values</returns>
        public uint[] ReadUnsigned(string[] RegNames, uint NumCaptures)
        {
            return ReadUnsigned(RegNames, NumCaptures, 1);
        }

        /// <summary>
        /// Read set of multiple unsigned registers, numCaptures times per data ready, numBuffers total captures
        /// </summary>
        /// <param name="RegNames">Names of all registers to read</param>
        /// <param name="NumCaptures">Number of times to read the register list</param>
        /// <param name="NumBuffers">Number of register captures to read</param>
        /// <returns>Array of register read values</returns>
        public uint[] ReadUnsigned(string[] RegNames, uint NumCaptures, uint NumBuffers)
        {
            List<RegClass> RegList = new List<RegClass>();
            foreach (string name in RegNames)
            {
                RegList.Add(m_RegMap[name]);
            }
            return Dut.ReadUnsigned(RegList, NumCaptures, NumBuffers);
        }

        /// <summary>
        /// Read single signed register
        /// </summary>
        /// <param name="RegName">Name of register to read. Must be in RegMap</param>
        /// <returns>Signed register value</returns>
        public long ReadSigned(string RegName)
        {
            return Dut.ReadSigned(m_RegMap[RegName]);
        }

        /// <summary>
        /// Read multiple signed registers
        /// </summary>
        /// <param name="RegNames">Names of all registers to read</param>
        /// <returns>Array of register read values</returns>
        public long[] ReadSigned(string[] RegNames)
        {
            return ReadSigned(RegNames, 1, 1);
        }

        /// <summary>
        /// Read set of multiple signed registers, numCaptures times
        /// </summary>
        /// <param name="RegNames">Names of all registers to read</param>
        /// <param name="NumCaptures">Number of times to read the register list</param>
        /// <returns>Array of register read values</returns>
        public long[] ReadSigned(string[] RegNames, uint NumCaptures)
        {
            return ReadSigned(RegNames, NumCaptures, 1);
        }

        /// <summary>
        /// Read set of multiple signed registers, numCaptures times per data ready, numBuffers total captures
        /// </summary>
        /// <param name="RegNames">Names of all registers to read</param>
        /// <param name="NumCaptures">Number of times to read the register list</param>
        /// <param name="NumBuffers">Number of register captures to read</param>
        /// <returns>Array of register read values</returns>
        public long[] ReadSigned(string[] RegNames, uint NumCaptures, uint NumBuffers)
        {
            List<RegClass> RegList = new List<RegClass>();
            foreach (string name in RegNames)
            {
                RegList.Add(m_RegMap[name]);
            }
            return Dut.ReadSigned(RegList, NumCaptures, NumBuffers);
        }

        #endregion

        #region "Register Write"

        /// <summary>
        /// Write an unsigned value to a single register in the RegMap
        /// </summary>
        /// <param name="RegName">Name of register to write</param>
        /// <param name="WriteData">Data to write to the register</param>
        public void WriteUnsigned(string RegName, uint WriteData)
        {
            Dut.WriteUnsigned(m_RegMap[RegName], WriteData);
        }

        /// <summary>
        /// Write unsigned values to multiple registers in the RegMap
        /// </summary>
        /// <param name="RegNames">Names of registers to write</param>
        /// <param name="WriteData">Data to write to the registers</param>
        public void WriteUnsigned(string[] RegNames, uint[] WriteData)
        {
            List<RegClass> RegList = new List<RegClass>();
            foreach (string name in RegNames)
            {
                RegList.Add(m_RegMap[name]);
            }
            Dut.WriteUnsigned(RegList, WriteData);
        }

        /// <summary>
        /// Write a signed value to a single register in the RegMap
        /// </summary>
        /// <param name="RegName">Name of register to write</param>
        /// <param name="WriteData">Data to write to the register</param>
        public void WriteSigned(string RegName, int WriteData)
        {
            Dut.WriteSigned(m_RegMap[RegName], WriteData);
        }

        /// <summary>
        /// Write signed values to multiple registers in the RegMap
        /// </summary>
        /// <param name="RegNames">Names of registers to write</param>
        /// <param name="WriteData">Data to write to the registers</param>
        public void WriteSigned(string[] RegNames, int[] WriteData)
        {
            List<RegClass> RegList = new List<RegClass>();
            foreach (string name in RegNames)
            {
                RegList.Add(m_RegMap[name]);
            }
            Dut.WriteSigned(RegList, WriteData);
        }

        /// <summary>
        /// Write a single unsigned register, based on register addr/page
        /// </summary>
        /// <param name="RegAddr">register address to write to</param>
        /// <param name="RegPage">Register page</param>
        /// <param name="WriteData">Data to write to the register</param>
        public void WriteUnsigned(uint RegAddr, uint RegPage, uint WriteData)
        {
            WriteUnsigned(new[] { RegAddr }, RegPage, new[] { WriteData });
        }

        /// <summary>
        /// Write unsigned data based on register addr/page
        /// </summary>
        /// <param name="RegAddrs">Array of register addresses to write to</param>
        /// <param name="RegPage">Register page</param>
        /// <param name="WriteData">Data to write to the registers. Must be same size as RegAddrs</param>
        public void WriteUnsigned(uint[] RegAddrs, uint RegPage, uint[] WriteData)
        {
            List<RegClass> regs = new List<RegClass>();
            foreach (uint addr in RegAddrs)
            {
                regs.Add(new RegClass { Address = addr, Page = RegPage, NumBytes = m_regSize });
            }
            Dut.WriteUnsigned(regs, WriteData);
        }

        /// <summary>
        /// Write a single signed register, based on register addr/page
        /// </summary>
        /// <param name="RegAddr">register address to write to</param>
        /// <param name="RegPage">Register page</param>
        /// <param name="WriteData">Data to write to the register</param>
        public void WriteSigned(uint RegAddr, uint RegPage, int WriteData)
        {
            WriteSigned(new[] { RegAddr }, RegPage, new[] { WriteData });
        }

        /// <summary>
        /// Write signed data based on register addr/page
        /// </summary>
        /// <param name="RegAddrs">Array of register addresses to write to</param>
        /// <param name="RegPage">Register page</param>
        /// <param name="WriteData">Data to write to the registers. Must be same size as RegAddrs</param>
        public void WriteSigned(uint[] RegAddrs, uint RegPage, int[] WriteData)
        {
            List<RegClass> regs = new List<RegClass>();
            foreach (uint addr in RegAddrs)
            {
                regs.Add(new RegClass { Address = addr, Page = RegPage, NumBytes = m_regSize });
            }
            Dut.WriteSigned(regs, WriteData);
        }

        #endregion

        #region "Register Stream"

        /// <summary>
        /// Start an asynchronous buffered register read stream
        /// </summary>
        /// <param name="RegNames">List of register names to read</param>
        /// <param name="NumCaptures">Number of times to read register list per data ready</param>
        /// <param name="NumBuffers">Total number of captures to perform</param>
        /// <param name="TimeoutSeconds">Stream timeout time, in seconds</param>
        public void StartBufferedStream(string[] RegNames, uint NumCaptures, uint NumBuffers, int TimeoutSeconds)
        {
            List<RegClass> RegList = new List<RegClass>();
            foreach (string name in RegNames)
            {
                RegList.Add(m_RegMap[name]);
            }
            m_StreamRegs = RegList;
            Dut.StartBufferedStream(RegList, NumCaptures, NumBuffers, TimeoutSeconds, null);
        }

        /// <summary>
        /// Start an asynchronous buffered register read stream based on register address/page
        /// </summary>
        /// <param name="RegAddrs">Array of register addresses to read</param>
        /// <param name="RegPage">Register page</param>
        /// <param name="NumCaptures">Number of times to read register list per data ready</param>
        /// <param name="NumBuffers">Total number of captures to perform</param>
        /// <param name="TimeoutSeconds">Stream timeout time, in seconds</param>
        public void StartBufferedStream(uint[] RegAddrs, uint RegPage, uint NumCaptures, uint NumBuffers, int TimeoutSeconds)
        {
            List<RegClass> RegList = new List<RegClass>();
            foreach (uint addr in RegAddrs)
            {
                RegList.Add(new RegClass { Address = addr, Page = RegPage, NumBytes = m_regSize });
            }
            m_StreamRegs = RegList;
            Dut.StartBufferedStream(RegList, NumCaptures, NumBuffers, TimeoutSeconds, null);
        }

        /// <summary>
        /// Get a buffered stream data packet
        /// </summary>
        /// <returns>A single buffer from a stream. Will be null if no data available</returns>
        public ushort[] GetBufferedStreamDataPacket()
        {
            return Dut.GetBufferedStreamDataPacket();
        }

        /// <summary>
        /// Converted buffered stream data packet to 32-bit array, based on the size of the registers read
        /// </summary>
        /// <param name="buf">Raw buffer packet to convert</param>
        /// <returns>32-bit unsigned array representing the value of each register read</returns>
        public uint[] ConvertBufferDataToU32(ushort[] buf)
        {
            return Dut.ConvertReadDataToU32(m_StreamRegs, buf);
        }

        #endregion

        /// <summary>
        /// Connect to FX3 board
        /// </summary>
        private void ConnectToBoard()
        {
            FX3.WaitForBoard(2);
            if (FX3.AvailableFX3s.Count() > 0)
            {
                FX3.Connect(FX3.AvailableFX3s[0]);
            }
            else if (FX3.BusyFX3s.Count() > 0)
            {
                FX3.ResetAllFX3s();
                FX3.WaitForBoard(5);
                ConnectToBoard();
            }
            else
            {
                throw new Exception("No FX3 board connected!");
            }
        }
    }
}