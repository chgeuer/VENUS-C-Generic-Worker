//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





#region Using statements

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Management;

#endregion

namespace Microsoft.EMIC.Cloud.Utilities
{
    internal sealed class ConvUtil
    {
        private ConvUtil() { }

        internal static IPAddress IPAddressFromIPv4Int32(uint addr)
        {
            byte[] address = new byte[4];

            address[0] = ((byte)(addr));
            address[1] = ((byte)(addr >> 8));
            address[2] = ((byte)(addr >> 16));
            address[3] = ((byte)(addr >> 24));

            return new System.Net.IPAddress(address);
        }

        internal static int PortFromBytes(byte highByte, byte lowByte)
        {
            int port = (int)((int)highByte << 8) | ((int)lowByte);

            return port;
        }

        internal static void GetWMIDataForPid(int pid, out string username, out string processName)
        {
            // ms-help://MS.MSDNQTR.v80.en/MS.MSDN.v80/MS.WIN32COM.v10.en/wmisdk/wmi/win32_process.htm
            const string WMI_Query = "Select * from Win32_Process where ProcessId={0}";

            SelectQuery selectQuery = new SelectQuery(String.Format(WMI_Query, pid));

            using (ManagementObjectSearcher mos = new ManagementObjectSearcher(selectQuery))
            {
                foreach (ManagementObject mo in mos.Get())
                {
                    processName = mo.GetPropertyValue("ExecutablePath") as string;
                    if (processName == null)
                    {
                        processName = String.Empty;
                    }

                    string[] s = new String[2];
                    mo.InvokeMethod("GetOwner", (object[])s);
                    if (String.IsNullOrEmpty(s[0]))
                    {
                        username = String.Empty;
                    }
                    else
                    {
                        // DOMAIN\user
                        username = s[1].ToUpper() + "\\" + s[0].ToLower();
                    }

                    return;
                }
            }

            username = String.Empty;
            processName = String.Empty;
        }
    }
}
