//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





#region Using statements

using System;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

#endregion

namespace Microsoft.EMIC.Cloud.Utilities
{
    internal enum TCP_TABLE_CLASS
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL,
    }

    internal enum UDP_TABLE_CLASS
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCPTABLE_OWNER_PID
    {
        internal uint dwNumEntries;

        internal MIB_TCPROW_OWNER_PID table;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCP6TABLE_OWNER_PID
    {
        internal uint dwNumEntries;

        internal MIB_TCP6ROW_OWNER_PID table;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_UDP6TABLE_OWNER_PID
    {
        internal uint dwNumEntries;

        internal MIB_UDP6ROW_OWNER_PID table;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_UDPTABLE_OWNER_PID
    {
        internal uint dwNumEntries;

        internal MIB_UDPROW_OWNER_PID table;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCPROW_OWNER_PID
    {
        //DWORD dwState; 
        internal TcpState state;

        //DWORD dwLocalAddr; 
        internal uint localAddr;

        //DWORD dwLocalPort; 
        internal byte localPort1;
        internal byte localPort2;
        internal byte localPort3;
        internal byte localPort4;

        //DWORD dwRemoteAddr; 
        internal uint remoteAddr;

        //DWORD dwRemotePort; 
        internal byte remotePort1;
        internal byte remotePort2;
        internal byte remotePort3;
        internal byte remotePort4;

        //DWORD dwOwningPid;
        internal int owningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCP6ROW_OWNER_PID
    {
        // The IPv6 address of the local endpoint in the TCP link. 
        //UCHAR ucLocalAddr[16];  
        internal byte localAddr00;
        internal byte localAddr01;
        internal byte localAddr02;
        internal byte localAddr03;
        internal byte localAddr04;
        internal byte localAddr05;
        internal byte localAddr06;
        internal byte localAddr07;
        internal byte localAddr08;
        internal byte localAddr09;
        internal byte localAddr10;
        internal byte localAddr11;
        internal byte localAddr12;
        internal byte localAddr13;
        internal byte localAddr14;
        internal byte localAddr15;

        // Scope ID for the local IPv6 address. 
        //DWORD dwLocalScopeId;  
        internal uint localScopeId; //?

        // The number of the port used by the local endpoint. 
        //DWORD dwLocalPort; 
        internal byte localPort1;
        internal byte localPort2;
        internal byte localPort3;
        internal byte localPort4;

        // The IPv6 address of the remote endpoint in the TCP link. 
        //UCHAR ucRemoteAddr[16];  
        internal byte remoteAddr00;
        internal byte remoteAddr01;
        internal byte remoteAddr02;
        internal byte remoteAddr03;
        internal byte remoteAddr04;
        internal byte remoteAddr05;
        internal byte remoteAddr06;
        internal byte remoteAddr07;
        internal byte remoteAddr08;
        internal byte remoteAddr09;
        internal byte remoteAddr10;
        internal byte remoteAddr11;
        internal byte remoteAddr12;
        internal byte remoteAddr13;
        internal byte remoteAddr14;
        internal byte remoteAddr15;

        // Scope ID for the remote IPv6 address. 
        //DWORD dwRemoteScopeId;  
        internal uint remoteScopeId; //?

        // The number of the port used by the remote endpoint. 
        //DWORD dwRemotePort; 
        internal byte remotePort1;
        internal byte remotePort2;
        internal byte remotePort3;
        internal byte remotePort4;

        // State of the TCP connection. 
        //DWORD dwState; 
        internal TcpState state;

        // The PID of the process that issued a context bind for this TCP link. 
        //DWORD dwOwningPid;
        internal int owningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_UDPROW_OWNER_PID
    {
        //DWORD dwLocalAddr; 
        internal uint localAddr;

        //DWORD dwLocalPort; 
        internal byte localPort1;
        internal byte localPort2;
        internal byte localPort3;
        internal byte localPort4;

        //DWORD dwOwningPid;
        internal int owningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_UDP6ROW_OWNER_PID
    {
        // Local IPv6 address for the UDP endpoint. 
        // UCHAR ucLocalAddr[16];  
        internal byte localAddr00;
        internal byte localAddr01;
        internal byte localAddr02;
        internal byte localAddr03;
        internal byte localAddr04;
        internal byte localAddr05;
        internal byte localAddr06;
        internal byte localAddr07;
        internal byte localAddr08;
        internal byte localAddr09;
        internal byte localAddr10;
        internal byte localAddr11;
        internal byte localAddr12;
        internal byte localAddr13;
        internal byte localAddr14;
        internal byte localAddr15;

        // Scope ID for the local IPv6 address of the UDP endpoint. 
        // DWORD dwLocalScopeId;  
        internal uint localScopeId; //?

        // Local port number for the UDP endpoint. 
        // DWORD dwLocalPort;  
        internal byte localPort1;
        internal byte localPort2;
        internal byte localPort3;
        internal byte localPort4;

        // The PID of the process that issued a context bind for this endpoint. If this value is set to 0, the information for this endpoint is unavailable. 
        // DWORD dwOwningPid;
        internal int owningPid;
    }
}
