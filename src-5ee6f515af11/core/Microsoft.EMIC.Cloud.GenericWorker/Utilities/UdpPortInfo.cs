//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





#region Using statements

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

#endregion

namespace Microsoft.EMIC.Cloud.Utilities
{
    /// <summary>
    /// Wrapper for MIB_UDPROW_OWNER_PID and MIB_UDP6ROW_OWNER_PID
    /// </summary>
    public sealed class UdpPortInfo
    {
        #region Constructors

        internal UdpPortInfo(MIB_UDPROW_OWNER_PID u) : this(u, false) { }

        internal UdpPortInfo(MIB_UDPROW_OWNER_PID u, bool determineUserAndProcess)
        {
            this._localPort = ConvUtil.PortFromBytes(u.localPort1, u.localPort2);

            this._localAddr = ConvUtil.IPAddressFromIPv4Int32(u.localAddr);
            
            this._owningPid = u.owningPid;
            if (determineUserAndProcess)
            {
                string user = this.Username;
                string proc = this.ProcessName;
            }
        }

        internal UdpPortInfo(MIB_UDP6ROW_OWNER_PID u) : this(u, false) { }

        internal UdpPortInfo(MIB_UDP6ROW_OWNER_PID u, bool determineUserAndProcess)
        {
            this._localPort = ConvUtil.PortFromBytes(u.localPort1, u.localPort2);

            byte[] a = new byte[] { 
                u.localAddr00, u.localAddr01, u.localAddr02, u.localAddr03, 
                u.localAddr04, u.localAddr05, u.localAddr06, u.localAddr07, 
                u.localAddr08, u.localAddr09, u.localAddr10, u.localAddr11, 
                u.localAddr12, u.localAddr13, u.localAddr14, u.localAddr15 };

            this._localAddr = new IPAddress(a, (long)u.localScopeId);

            this._owningPid = u.owningPid;
            if (determineUserAndProcess)
            {
                string user = this.Username;
                string proc = this.ProcessName;
            }
        }

        #endregion

        #region Properties

        #region Local endpoint

        /// <summary>
        /// Gets the local IP address.
        /// </summary>
        /// <value>The local IP address.</value>
        public IPAddress LocalIPAddress { get { return _localAddr; } }
        private IPAddress _localAddr;

        /// <summary>
        /// Gets the local UDP port.
        /// </summary>
        /// <value>The local UDP port.</value>
        public int LocalPort { get { return _localPort; } }
        private int _localPort;

        /// <summary>
        /// Gets the local IP end point.
        /// </summary>
        /// <value>The local IP end point.</value>
        public IPEndPoint LocalIPEndPoint
        {
            get { return new IPEndPoint(_localAddr, _localPort); }
        }

        #endregion

        #region Username

        /// <summary>
        /// Gets the username.
        /// </summary>
        /// <value>The username.</value>
        public string Username
        {
            get
            {
                if (this._username == null)
                {
                    ConvUtil.GetWMIDataForPid(this.OwningPID, out this._username, out this._processname);
                }
                return this._username;
            }
        }
        private string _username;

        #endregion

        #region OwningPID & ProcessName

        /// <summary>
        /// Gets the owning PID.
        /// </summary>
        /// <value>The owning PID.</value>
        public int OwningPID { get { return _owningPid; } }
        private int _owningPid;

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        /// <value>The name of the process.</value>
        public string ProcessName
        {
            get
            {
                if (this._processname == null)
                {

                    ConvUtil.GetWMIDataForPid(this.OwningPID, out this._username, out this._processname);
                }
                return this._processname;
            }
        }
        private string _processname;

        #endregion

        #endregion

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return String.Format("UDP {0}", 
                this.LocalIPEndPoint.ToString());
        }
    }
}
