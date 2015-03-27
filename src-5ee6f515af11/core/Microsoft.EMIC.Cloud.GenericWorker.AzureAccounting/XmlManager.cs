//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting
{
    /// <summary>
    /// Handles XML data in order to submit them for the client's requests.
    /// </summary>
    public class XmlManager
    {
        /// <summary>
        /// Creates the appropriate XML file for POST and PUT requests.
        /// </summary>
        /// <param name="usageRecord">The usage record that needs to be inserted or updated.</param>
        /// <returns>An XML file containing the proper data for submission in a string format.</returns>
        public static string CreateXml(UsageRecord usageRecord)
        {
            const string pattern = "yyyy-MM-ddTHH:mm:ss'Z'";

            XDocument xml = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("usageRecord",
                    new XElement("id", usageRecord.ID),
                    new XElement("consumerId", usageRecord.ConsumerID),
                    new XElement("createTime", usageRecord.CreateTime.ToString(pattern)),
                    new XElement("creatorId", usageRecord.CreatorID),
                    new XElement("startTime", usageRecord.StartTime.ToString(pattern)),
                    new XElement("endTime", usageRecord.EndTime.ToString(pattern)),
                    new XElement("resourceOwner", usageRecord.ResourceOwner),
                    new XElement("resourceType", usageRecord.ResourceType)
                )
            );

            if (usageRecord.CustomProperties.Count > 0)
            {
                XElement customProperties = new XElement("customProperties");

                foreach (KeyValuePair<string, string> pair in usageRecord.CustomProperties)
                {
                    XElement entry = new XElement("entry",
                            new XElement("key", pair.Key),
                            new XElement("value", pair.Value)
                        );

                    customProperties.Add(entry);
                }

                xml.Element("usageRecord").Add(customProperties);
            }

            if (usageRecord.ResourceSpecificProperties.Count > 0)
            {
                XElement resourceSpecificProperties = new XElement("resourceSpecificProperties");

                foreach (KeyValuePair<string, string> pair in usageRecord.ResourceSpecificProperties)
                {
                    XElement entry = new XElement("entry",
                            new XElement("key", pair.Key),
                            new XElement("value", pair.Value)
                        );

                    resourceSpecificProperties.Add(entry);
                }

                xml.Element("usageRecord").Add(resourceSpecificProperties);
            }

            if (usageRecord is VmUsage)
            {
                VmUsage vm = usageRecord as VmUsage;

                xml.Element("usageRecord").Add(
                    new XElement("refHost", vm.RefHost),
                    new XElement("refVM", vm.RefVM),
                    new XElement("usageStart", vm.UsageStart.ToString(pattern)),
                    new XElement("usageEnd", vm.UsageEnd.ToString(pattern))
                );
            }

            if (usageRecord is NetworkUsage)
            {
                NetworkUsage network = usageRecord as NetworkUsage;

                xml.Element("usageRecord").Add(
                    new XElement("periodNetworkIn", network.PeriodNetworkIn),
                    new XElement("periodNetworkOut", network.PeriodNetworkOut),
                    new XElement("overallNetworkIn", network.OverallNetworkIn),
                    new XElement("overallNetworkOut", network.OverallNetworkOut),
                    new XElement("refVM", network.RefVM)
                );
            }

            if (usageRecord is StorageUsage)
            {
                StorageUsage storage = usageRecord as StorageUsage;

                xml.Element("usageRecord").Add(
                    new XElement("itemCount", storage.ItemCount),
                    new XElement("storageTransactions", storage.StorageTransactions),
                    new XElement("storageVolume", storage.StorageVolume)
                );
            }

            if (usageRecord is JobResourceUsage)
            {
                JobResourceUsage storage = usageRecord as JobResourceUsage;

                xml.Element("usageRecord").Add(
                    new XElement("cores", storage.NumberofCores),
                    new XElement("cpuDuration", storage.CPUDuration),
                    new XElement("disk", storage.Disk),
                    new XElement("inputFilesNumber", storage.NumberOfInputFiles),
                    new XElement("inputFilesSize", storage.SizeOfInputFiles),
                    new XElement("jobEnd", storage.JobEndTime.ToString(pattern)),
                    new XElement("jobId", storage.JobID),
                    new XElement("jobName", storage.JobName),
                    new XElement("jobStart", storage.JobStartTime.ToString(pattern)),
                    new XElement("jobStatus", storage.Status),
                    new XElement("memory", storage.Memory),
                    new XElement("overallNetworkIn", storage.NetworkIn),
                    new XElement("overallNetworkOut", storage.NetworkOut),
                    new XElement("outputFilesNumber", storage.NumberOfOutputFiles),
                    new XElement("outputFilesSize", storage.SizeOfOutputFiles),
                    new XElement("processors", storage.Processors),
                    new XElement("refVM", storage.refVM),
                    new XElement("wallDuration", storage.WallDuration)
                );
            }

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        public static List<UsageRecord> ParseXML(string doc)
        {
            XDocument xmlDoc = XDocument.Parse(doc);

            var xmlRecords = xmlDoc.Elements("rawUsageRecords").Elements("usageRecord");
            List<UsageRecord> resultSet = new List<UsageRecord>();

            foreach (var xmlRecord in xmlRecords)
            {
                UsageRecord usageRecord = null;
                if (xmlRecord.Element("usageStart") != null)
                {
                    var vmRecord = new VmUsage();
                    vmRecord.RefHost = xmlRecord.Element("refHost").Value;
                    vmRecord.RefVM = xmlRecord.Element("refVM").Value;
                    vmRecord.UsageStart = DateTime.Parse(xmlRecord.Element("usageStart").Value);
                    vmRecord.UsageEnd = DateTime.Parse(xmlRecord.Element("usageEnd").Value);

                    usageRecord = vmRecord;
                }
                else if (xmlRecord.Element("storageVolume") != null)
                {
                    var storageRecord = new StorageUsage();
                    storageRecord.ItemCount = xmlRecord.Element("itemCount").Value;
                    storageRecord.StorageTransactions = xmlRecord.Element("storageTransactions").Value;
                    storageRecord.StorageVolume = xmlRecord.Element("storageVolume").Value;

                    usageRecord = storageRecord;
                }
                else if (xmlRecord.Element("overallNetworkIn") != null)
                {
                    var networkRecord = new NetworkUsage();

                    networkRecord.PeriodNetworkIn = xmlRecord.Element("periodNetworkIn").Value;
                    networkRecord.PeriodNetworkOut = xmlRecord.Element("periodNetworkOut").Value;
                    networkRecord.OverallNetworkIn = xmlRecord.Element("overallNetworkIn").Value;
                    networkRecord.OverallNetworkOut = xmlRecord.Element("overallNetworkOut").Value;
                    networkRecord.PeriodNetworkIn = xmlRecord.Element("refVM").Value;

                    usageRecord = networkRecord;
                }
                else if (xmlRecord.Element("processors") != null)
                {
                    var jobRecord = new JobResourceUsage();

                    jobRecord.NumberofCores = int.Parse(xmlRecord.Element("cores").Value);
                    jobRecord.CPUDuration = int.Parse(xmlRecord.Element("cpuDuration").Value);
                    jobRecord.Disk = int.Parse(xmlRecord.Element("disk").Value);
                    jobRecord.NumberOfInputFiles = int.Parse(xmlRecord.Element("inputFilesNumber").Value);
                    jobRecord.SizeOfInputFiles = long.Parse(xmlRecord.Element("inputFilesSize").Value);
                    jobRecord.JobEndTime = DateTime.Parse(xmlRecord.Element("jobEnd").Value);
                    jobRecord.JobID = xmlRecord.Element("jobId").Value;
                    jobRecord.JobName = xmlRecord.Element("jobName").Value;
                    jobRecord.JobStartTime = DateTime.Parse(xmlRecord.Element("jobStart").Value);   
                    jobRecord.Status = xmlRecord.Element("jobStatus").Value;
                    jobRecord.Memory = int.Parse(xmlRecord.Element("memory").Value);
                    jobRecord.NetworkIn = int.Parse(xmlRecord.Element("overallNetworkIn").Value);
                    jobRecord.NetworkOut = int.Parse(xmlRecord.Element("overallNetworkOut").Value);
                    jobRecord.NumberOfOutputFiles = int.Parse(xmlRecord.Element("outputFilesNumber").Value);
                    jobRecord.SizeOfOutputFiles = int.Parse(xmlRecord.Element("outputFilesSize").Value);
                    jobRecord.Processors = int.Parse(xmlRecord.Element("processors").Value);
                    jobRecord.refVM = xmlRecord.Element("refVM").Value;
                    jobRecord.WallDuration = int.Parse(xmlRecord.Element("wallDuration").Value);
                   
                }
                else
                {
                    usageRecord = new UsageRecord();
                }

                usageRecord.ID = xmlRecord.Element("id").Value;
                usageRecord.ConsumerID = xmlRecord.Element("consumerId").Value;
                usageRecord.CreateTime = DateTime.Parse(xmlRecord.Element("createTime").Value);
                usageRecord.CreatorID = xmlRecord.Element("creatorId").Value;
                usageRecord.StartTime = DateTime.Parse(xmlRecord.Element("startTime").Value);
                usageRecord.EndTime = DateTime.Parse(xmlRecord.Element("endTime").Value);
                usageRecord.ResourceOwner = xmlRecord.Element("resourceOwner").Value;
                usageRecord.ResourceType = xmlRecord.Element("resourceType").Value;

                usageRecord.ResourceSpecificProperties = new Dictionary<string, string>();

                foreach (var entry in xmlRecord.Element("resourceSpecificProperties").Elements("entry"))
                {
                    usageRecord.ResourceSpecificProperties.Add(entry.Element("key").Value, entry.Element("value").Value);
                }

                usageRecord.CustomProperties = new Dictionary<string, string>();

                foreach (var entry in xmlRecord.Element("customProperties").Elements("entry"))
                {
                    usageRecord.ResourceSpecificProperties.Add(entry.Element("key").Value, entry.Element("value").Value);
                }

                resultSet.Add(usageRecord);
            }

            return resultSet;
        }
    }
}
