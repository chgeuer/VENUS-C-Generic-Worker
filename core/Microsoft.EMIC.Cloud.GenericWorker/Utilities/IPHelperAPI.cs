//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





#region Using statements

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Permissions;

#endregion

namespace Microsoft.EMIC.Cloud.Utilities
{
    /// <summary>
    /// TCP/IP Helper API wrapper
    /// </summary>
    public static class IPHelperAPI
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable, ref int dwOutBufLen, bool sort,
            int ipVersion, TCP_TABLE_CLASS tblClass, int reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(
            IntPtr pUdpTable, ref int dwOutBufLen, bool sort,
            int ipVersion, UDP_TABLE_CLASS tblClass, int reserved);


        internal sealed class IPVersion
        {
            internal const int AF_INET = 2; //Internetwork: UDP, TCP, etc.
            internal const int AF_INET6 = 23; //Internetwork Version 6
        }

        internal sealed class Error
        {
            internal const int ERROR_INSUFFICIENT_BUFFER = 122;
            internal const int ERROR_INVALID_PARAMETER = 87;
            internal const int NO_ERROR = 0;

            internal static void ThrowAppropriateException(uint errorCode)
            {
                if (errorCode == Error.NO_ERROR) 
                {
                    return;
                }
                if (errorCode == Error.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new NotSupportedException(ExceptionMessages.ErrorInsufficentBuffer);
                }
                if (errorCode == Error.ERROR_INVALID_PARAMETER)
                {
                    throw new NotSupportedException(ExceptionMessages.ErrorInvalidParameter);
                }

                throw new NotSupportedException(String.Format(
                    ExceptionMessages.UnknownErrorCode, errorCode));
            }
        }

        /// <summary>
        /// Gets all TCP connections.
        /// </summary>
        /// <returns></returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        public static ICollection<TcpPortInfo> GetAllTcp4Connections()
        {
            List<TcpPortInfo> tTable;
            int buffSize = 0;

            // how much memory do we need?
            uint ret = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, IPVersion.AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);

            try
            {
                ret = GetExtendedTcpTable(buffTable, ref buffSize, true,
                    IPVersion.AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != Error.NO_ERROR)
                {
                    Error.ThrowAppropriateException(ret);
                }

                MIB_TCPTABLE_OWNER_PID tab = (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(buffTable, typeof(MIB_TCPTABLE_OWNER_PID));
                IntPtr rowPtr = (IntPtr)((long)buffTable + Marshal.SizeOf(tab.dwNumEntries));
                tTable = new List<TcpPortInfo>();

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    MIB_TCPROW_OWNER_PID tcpRow = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    tTable.Add(new TcpPortInfo(tcpRow));
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));
                }
            }
            finally
            {
                // Free the Memory
                Marshal.FreeHGlobal(buffTable);
            }
            return tTable;

        }

        /// <summary>
        /// Gets all IPv6 TCP connections.
        /// </summary>
        /// <returns></returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public static ICollection<TcpPortInfo> GetAllTcp6Connections()
        {
            List<TcpPortInfo> tTable;
            int buffSize = 0;

            // how much memory do we need?
            uint ret = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, IPVersion.AF_INET6,
                TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);

            try
            {
                ret = GetExtendedTcpTable(buffTable, ref buffSize, true,
                    IPVersion.AF_INET6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != Error.NO_ERROR)
                {
                    Error.ThrowAppropriateException(ret);
                }

                MIB_TCP6TABLE_OWNER_PID tab = (MIB_TCP6TABLE_OWNER_PID)
                    Marshal.PtrToStructure(buffTable, typeof(MIB_TCP6TABLE_OWNER_PID));

                IntPtr rowPtr = (IntPtr)((long)buffTable + Marshal.SizeOf(tab.dwNumEntries));

                tTable = new List<TcpPortInfo>();

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    MIB_TCP6ROW_OWNER_PID tcpRow = (MIB_TCP6ROW_OWNER_PID)
                        Marshal.PtrToStructure(rowPtr, typeof(MIB_TCP6ROW_OWNER_PID));

                    tTable.Add(new TcpPortInfo(tcpRow));
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));   // next entry
                }
            }
            finally
            {
                // Free the Memory
                Marshal.FreeHGlobal(buffTable);
            }
            return tTable;
        }

        /// <summary>
        /// Gets all IPv4 UDP connections.
        /// </summary>
        /// <returns></returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public static ICollection<UdpPortInfo> GetAllUdp4Connections()
        {
            List<UdpPortInfo> tTable = new List<UdpPortInfo>();
            int buffSize = 0;

            // how much memory do we need?
            uint ret = GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, IPVersion.AF_INET,
               UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);

            try
            {
                ret = GetExtendedUdpTable(buffTable, ref buffSize, true, IPVersion.AF_INET,
                    UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
                if (ret != Error.NO_ERROR)
                {
                    Error.ThrowAppropriateException(ret);
                }

                MIB_UDPTABLE_OWNER_PID tab = (MIB_UDPTABLE_OWNER_PID)
                    Marshal.PtrToStructure(buffTable, typeof(MIB_UDPTABLE_OWNER_PID));

                IntPtr rowPtr = (IntPtr)((long)buffTable + Marshal.SizeOf(tab.dwNumEntries));

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    MIB_UDPROW_OWNER_PID udpRow = (MIB_UDPROW_OWNER_PID)
                        Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_PID));

                    tTable.Add(new UdpPortInfo(udpRow));
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(udpRow));   // next entry
                }
            }
            finally
            {
                // Free the Memory
                Marshal.FreeHGlobal(buffTable);
            }
            return tTable;
        }

        /// <summary>
        /// Gets all IPv6 UDP connections.
        /// </summary>
        /// <returns></returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public static ICollection<UdpPortInfo> GetAllUdp6Connections()
        {
            List<UdpPortInfo> tTable = new List<UdpPortInfo>();
            int buffSize = 0;

            // how much memory do we need?
            uint ret = GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, IPVersion.AF_INET6,
               UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);

            try
            {
                ret = GetExtendedUdpTable(buffTable, ref buffSize, true, IPVersion.AF_INET6,
                    UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
                if (ret != Error.NO_ERROR)
                {
                    Error.ThrowAppropriateException(ret);
                }

                MIB_UDP6TABLE_OWNER_PID tab = (MIB_UDP6TABLE_OWNER_PID)
                    Marshal.PtrToStructure(buffTable, typeof(MIB_UDP6TABLE_OWNER_PID));

                IntPtr rowPtr = (IntPtr)((long)buffTable + Marshal.SizeOf(tab.dwNumEntries));

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    MIB_UDP6ROW_OWNER_PID udpRow = (MIB_UDP6ROW_OWNER_PID)
                        Marshal.PtrToStructure(rowPtr, typeof(MIB_UDP6ROW_OWNER_PID));

                    tTable.Add(new UdpPortInfo(udpRow));
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(udpRow));   // next entry
                }
            }
            finally
            {
                // Free the Memory
                Marshal.FreeHGlobal(buffTable);
            }
            return tTable;
        }

        /// <summary>
        /// Gets all IPv4 and IPv6 TCP connections.
        /// </summary>
        /// <returns></returns>
        public static ICollection<TcpPortInfo> GetAllTcpConnections()
        {
            List<TcpPortInfo> result = new List<TcpPortInfo>();
            result.AddRange(GetAllTcp4Connections());
            result.AddRange(GetAllTcp6Connections());
            return result;
        }

        /// <summary>
        /// Gets all IPv4 and IPv6 UDP connections.
        /// </summary>
        /// <returns></returns>
        public static ICollection<UdpPortInfo> GetAllUdpConnections()
        {
            List<UdpPortInfo> result = new List<UdpPortInfo>();
            result.AddRange(GetAllUdp4Connections());
            result.AddRange(GetAllUdp6Connections());
            return result;
        }

        /// <summary>
        /// Gets the TCP connection.
        /// </summary>
        /// <param name="localTcpPortNumber">The local TCP port number.</param>
        /// <returns></returns>
        public static TcpPortInfo GetTcpConnection(int localTcpPortNumber)
        {
            ICollection<TcpPortInfo> connections = GetAllTcpConnections();
            foreach (TcpPortInfo connection in connections)
            {
                if (connection.LocalPort == localTcpPortNumber)
                {
                    return connection;
                }
            }

            return null;
        }
    }
}
