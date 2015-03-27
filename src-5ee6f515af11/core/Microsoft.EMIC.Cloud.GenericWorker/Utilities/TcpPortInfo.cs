//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





#region Using statements

using System;
using System.Net;
using System.Net.NetworkInformation;

#endregion

namespace Microsoft.EMIC.Cloud.Utilities
{
    /// <summary>
    /// Wrapper structure for MIB_TCPROW_OWNER_PID and MIB_TCP6ROW_OWNER_PID
    /// </summary>
    public sealed class TcpPortInfo
    {
        #region Constructors

        private TcpPortInfo() { }

        internal TcpPortInfo(MIB_TCPROW_OWNER_PID c) : this(c, false) { }

        internal TcpPortInfo(MIB_TCPROW_OWNER_PID c, bool determineUserAndProcess)
        {
            this._localAddr = ConvUtil.IPAddressFromIPv4Int32(c.localAddr);
            this._remoteAddr = ConvUtil.IPAddressFromIPv4Int32(c.remoteAddr);

            this._localPort = ConvUtil.PortFromBytes(c.localPort1, c.localPort2);
            this._remotePort = ConvUtil.PortFromBytes(c.remotePort1, c.remotePort2);
            
            this._state = c.state;

            this._owningPid = c.owningPid;
            if (determineUserAndProcess)
            {
                string u = this.Username;
                string p = this.ProcessName;
            }
        }

        internal TcpPortInfo(MIB_TCP6ROW_OWNER_PID c) : this(c, false) { }

        internal TcpPortInfo(MIB_TCP6ROW_OWNER_PID c, bool determineUserAndProcess)
        {
            byte[] l = new byte[] { 
                c.localAddr00, c.localAddr01, c.localAddr02, c.localAddr03, 
                c.localAddr04, c.localAddr05, c.localAddr06, c.localAddr07, 
                c.localAddr08, c.localAddr09, c.localAddr10, c.localAddr11, 
                c.localAddr12, c.localAddr13, c.localAddr14, c.localAddr15 };
            this._localAddr = new IPAddress(l, (long)c.localScopeId);

            byte[] r = new byte[] { 
                c.remoteAddr00, c.remoteAddr01, c.remoteAddr02, c.remoteAddr03, 
                c.remoteAddr04, c.remoteAddr05, c.remoteAddr06, c.remoteAddr07, 
                c.remoteAddr08, c.remoteAddr09, c.remoteAddr10, c.remoteAddr11, 
                c.remoteAddr12, c.remoteAddr13, c.remoteAddr14, c.remoteAddr15 };
            this._remoteAddr = new IPAddress(r, (long)c.remoteScopeId);

            this._localPort = ConvUtil.PortFromBytes(c.localPort1, c.localPort2);
            this._remotePort = ConvUtil.PortFromBytes(c.remotePort1, c.remotePort2);

            this._state = c.state;

            this._owningPid = c.owningPid;
            if (determineUserAndProcess)
            {
                string u = this.Username;
                string p = this.ProcessName;
            }
        }

        #endregion

        #region Properties

        #region TcpState

        /// <summary>
        /// Gets the state of the TCP connection.
        /// </summary>
        /// <value>The state of the TCP.</value>
        public TcpState TcpState { get { return _state; } }
        private TcpState _state;

        #endregion

        #region Local endpoint

        /// <summary>
        /// Gets the local IP address.
        /// </summary>
        /// <value>The local IP address.</value>
        public IPAddress LocalIPAddress { get { return _localAddr; } }
        private IPAddress _localAddr;

        /// <summary>
        /// Gets the local TCP port.
        /// </summary>
        /// <value>The local TCP port.</value>
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

        #region Remote endpoint

        /// <summary>
        /// Gets the remote IP address.
        /// </summary>
        /// <value>The remote IP address.</value>
        public IPAddress RemoteIPAddress { get { return _localAddr; } }
        private IPAddress _remoteAddr;

        /// <summary>
        /// Gets the remote TCP port.
        /// </summary>
        /// <value>The remote TCP port.</value>
        public int RemotePort { get { return _remotePort; } }
        private int _remotePort;

        /// <summary>
        /// Gets the remote IP end point.
        /// </summary>
        /// <value>The remote IP end point.</value>
        public IPEndPoint RemoteIPEndPoint
        {
            get { return new IPEndPoint(_remoteAddr, _remotePort); }
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
            return String.Format("TCP {0} --> {1} ({2})",
                this.LocalIPEndPoint.ToString(),
                this.RemoteIPAddress.ToString(),
                this.TcpState.ToString());
        }
    }
}
