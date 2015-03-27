//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EMIC.Cloud;
using System.Xml;
using ConfigurationModifier.Models;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;

namespace ConfigurationModifier.Data
{
    [Export(typeof(XMLHandler))]
    public class XMLHandler
    {
        private string fileName;
        private Variables variables;

        private string _nameSpaceAbb;
        private string _nameSpace;
        private string _xPathSettingQuery;
        private string _xPathInstanceQuery;
        private string _xPathCertificatesQuery;
        private string _stsURLSuffix;
        private string _genericWorkerURLSuffix;
        private string _scalingServiceURLSuffix;
        private string _notificationURLSuffix;

        private string _tempFileName;

        public XMLHandler()
        {
            _nameSpaceAbb = ConfigurationManager.AppSettings["NameSpaceAbb"];
            _nameSpace = ConfigurationManager.AppSettings["NameSpace"];
            _xPathSettingQuery = ConfigurationManager.AppSettings["XPathSettingQuery"];
            _xPathInstanceQuery = ConfigurationManager.AppSettings["XPathInstanceQuery"];
            _xPathCertificatesQuery = ConfigurationManager.AppSettings["XPathCertificatesQuery"];
            _stsURLSuffix = ConfigurationManager.AppSettings["STSURLSuffix"];
            _genericWorkerURLSuffix = ConfigurationManager.AppSettings["GenericWorkerURLSuffix"];
            _scalingServiceURLSuffix = ConfigurationManager.AppSettings["ScalingServiceURLSuffix"];
            _notificationURLSuffix = ConfigurationManager.AppSettings["NotificationURLSuffix"];
            _tempFileName = ConfigurationManager.AppSettings["TempFileName"];

        }

        public string FileName
        {
            get
            {
                return this.fileName;
            }
            set
            {
                this.fileName = value;
                variables = null;
            }
        }

        public Variables XMLVariables
        {
            get
            {
                if (variables == null && File.Exists(FileName))
                {
                    variables = new Variables();
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(fileName);

                    XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDocument.NameTable);

                    nsMgr.AddNamespace(_nameSpaceAbb, _nameSpace);


                    bool boolValue = false;

                    Boolean.TryParse(xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.SecurityAllowInsecureAccess), nsMgr).Attributes["value"].Value, out boolValue);
                    variables.AllowInsecure = boolValue;
                    variables.ConnectionString = xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.GenericWorkerConnectionString), nsMgr).Attributes["value"].Value;
                    variables.DeploymentURL = xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.GenericWorkerURL), nsMgr).Attributes["value"].Value.Replace(_genericWorkerURLSuffix, "");
                    boolValue = false;
                    Boolean.TryParse(xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.GenericWorkerIsAccountingOn), nsMgr).Attributes["value"].Value, out boolValue);
                    variables.EnableAccounting = boolValue;
                    variables.EncryptedPassword = xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AccountEncryptedPassword), nsMgr).Attributes["value"].Value;
                    variables.ManagementThumbprint = xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AzureMngmtCertThumbprint), nsMgr).Attributes["value"].Value;
                    variables.RemoteAccountUserName = xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AccountUserName), nsMgr).Attributes["value"].Value;
                    variables.ServiceName = xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AzureServiceName), nsMgr).Attributes["value"].Value;
                    variables.STSThumbprint = xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.STSCertificateThumbprint), nsMgr).Attributes["value"].Value;
                    variables.SubscriptionId = xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AzureSubscriptionId), nsMgr).Attributes["value"].Value;

                    int intValue = 1;
                    int.TryParse(xmlDocument.SelectSingleNode(_xPathInstanceQuery, nsMgr).Attributes["count"].Value, out intValue);
                    variables.InstanceCount = intValue;
                }
                return variables;
            }
            set
            {
                variables = value;
            }
        }

        public void WriteToXML(string fileName)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(FileName);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDocument.NameTable);

            nsMgr.AddNamespace(_nameSpaceAbb, _nameSpace);

            //Instance Count
            xmlDocument.SelectSingleNode(_xPathInstanceQuery, nsMgr).Attributes["count"].Value = XMLVariables.InstanceCount.ToString();

            //ConnectionString
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.GenericWorkerConnectionString), nsMgr).Attributes["value"].Value = XMLVariables.ConnectionString;
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.DiagnosticsConnectionString), nsMgr).Attributes["value"].Value = XMLVariables.ConnectionString;
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.STSOnAzureConnectionString), nsMgr).Attributes["value"].Value = XMLVariables.ConnectionString;

            //STSThumbprint
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.STSCertificateThumbprint), nsMgr).Attributes["value"].Value = XMLVariables.STSThumbprint;
            xmlDocument.SelectSingleNode(String.Format(_xPathCertificatesQuery, CompositionIdentifiers.SSLCert), nsMgr).Attributes["thumbprint"].Value = XMLVariables.STSThumbprint;
            xmlDocument.SelectSingleNode(String.Format(_xPathCertificatesQuery, CompositionIdentifiers.STSCert), nsMgr).Attributes["thumbprint"].Value = XMLVariables.STSThumbprint;
            xmlDocument.SelectSingleNode(String.Format(_xPathCertificatesQuery, CompositionIdentifiers.PasswordEncrypCert), nsMgr).Attributes["thumbprint"].Value = XMLVariables.STSThumbprint;

            //ManagementThumbprint
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AzureMngmtCertThumbprint), nsMgr).Attributes["value"].Value = XMLVariables.ManagementThumbprint;
            xmlDocument.SelectSingleNode(String.Format(_xPathCertificatesQuery, CompositionIdentifiers.MgmtCert), nsMgr).Attributes["thumbprint"].Value = XMLVariables.ManagementThumbprint;

            //DeploymentURL
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.GenericWorkerURL), nsMgr).Attributes["value"].Value = XMLVariables.DeploymentURL + _genericWorkerURLSuffix;
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.STSURL), nsMgr).Attributes["value"].Value = XMLVariables.DeploymentURL + _stsURLSuffix;
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.ScalingServiceURL), nsMgr).Attributes["value"].Value = XMLVariables.DeploymentURL + _scalingServiceURLSuffix;
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.NotificationServiceURL), nsMgr).Attributes["value"].Value = XMLVariables.DeploymentURL + _notificationURLSuffix;

            //The Rest
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AccountUserName), nsMgr).Attributes["value"].Value = XMLVariables.RemoteAccountUserName;
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AccountEncryptedPassword), nsMgr).Attributes["value"].Value = XMLVariables.EncryptedPassword;
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.SecurityAllowInsecureAccess), nsMgr).Attributes["value"].Value = XMLVariables.AllowInsecure.ToString();
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.GenericWorkerIsAccountingOn), nsMgr).Attributes["value"].Value = XMLVariables.EnableAccounting.ToString();
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AzureServiceName), nsMgr).Attributes["value"].Value = XMLVariables.ServiceName;
            xmlDocument.SelectSingleNode(String.Format(_xPathSettingQuery, CompositionIdentifiers.AzureSubscriptionId), nsMgr).Attributes["value"].Value = XMLVariables.SubscriptionId;

            xmlDocument.Save(fileName);
        }

        public string WriteTempXML(string folderNameForTempFile)
        {

            string fileName = null;

            if (folderNameForTempFile.EndsWith("\\"))
            {
                fileName = folderNameForTempFile + _tempFileName + ".cscfg";
            }
            else
            {
                fileName = folderNameForTempFile + "\\" + _tempFileName + ".cscfg";
            }

            WriteToXML(fileName);

            return fileName;
        }

    }
}
