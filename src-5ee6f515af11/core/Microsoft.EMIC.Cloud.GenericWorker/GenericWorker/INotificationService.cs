//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OGF.BES;
using System.Xml;
using Microsoft.EMIC.Cloud.Notification;
using OGF.BES.Faults;

namespace Microsoft.EMIC.Cloud.GenericWorker
{

    /// <summary>
    /// Interface for the notification service
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName = "INotificationService")]
    public interface INotificationService
    {
        /// <summary>
        /// Creates a notification subscription for a given group of jobs on a given set of statuses.
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="statuses">The statuses.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://tempuri.org/INotificationService/CreateSubscriptionForGroupStatuses/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(InvalidRequestMessageFaultType), Action = "http://tempuri.org/INotificationService/CreateSubscriptionForGroupStatuses/" +
            "Fault/InvalidRequestMessageFaultType", Name = "InvalidRequestMessageFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://tempuri.org/INotificationService/CreateSubscriptionForGroupStatuses/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/INotificationService/CreateSubscriptionForGroupStatuses", ReplyAction = "http://tempuri.org/INotificationService/CreateSubscriptionForGroupStatusesResponse")]
        void CreateSubscriptionForGroupStatuses(string groupName, List<JobStatus> statuses, List<SerializableKeyValuePair<string, string>> pluginConfig);

        /// <summary>
        /// Creates the subscription on a job for a given set of statuses.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="statuses">The statuses.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CantApplyOperationToCurrentStateFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://tempuri.org/INotificationService/CreateSubscriptionForStatuses/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(InvalidRequestMessageFaultType), Action = "http://tempuri.org/INotificationService/CreateSubscriptionForStatuses/" +
            "Fault/InvalidRequestMessageFaultType", Name = "InvalidRequestMessageFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(CantApplyOperationToCurrentStateFaultType), Action = "http://tempuri.org/INotificationService/CreateSubscriptionForStatuses/" +
            "Fault/CantApplyOperationToCurrentStateFaultType", Name = "CantApplyOperationToCurrentStateFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://tempuri.org/INotificationService/CreateSubscriptionForStatuses/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/INotificationService/CreateSubscriptionForStatuses", ReplyAction = "http://tempuri.org/INotificationService/CreateSubscriptionForStatusesResponse")]
        void CreateSubscriptionForStatuses(EndpointReferenceType job, List<JobStatus> statuses, List<SerializableKeyValuePair<string, string>> pluginConfig);

        /// <summary>
        /// Creates the subscription on a job for all statuses.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CantApplyOperationToCurrentStateFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://tempuri.org/INotificationService/CreateSubscription/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(InvalidRequestMessageFaultType), Action = "http://tempuri.org/INotificationService/CreateSubscription/" +
            "Fault/InvalidRequestMessageFaultType", Name = "InvalidRequestMessageFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(CantApplyOperationToCurrentStateFaultType), Action = "http://tempuri.org/INotificationService/CreateSubscription/" +
            "Fault/CantApplyOperationToCurrentStateFaultType", Name = "CantApplyOperationToCurrentStateFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://tempuri.org/INotificationService/CreateSubscription/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/INotificationService/CreateSubscription", ReplyAction = "http://tempuri.org/INotificationService/CreateSubscriptionResponse")]
        void CreateSubscription(EndpointReferenceType job, List<SerializableKeyValuePair<string, string>> pluginConfig);

        /// <summary>
        /// Unsubscribes notification for the specified job.
        /// </summary>
        /// <param name="job">The job.</param>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://tempuri.org/INotificationService/Unsubscribe/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(InvalidRequestMessageFaultType), Action = "http://tempuri.org/INotificationService/Unsubscribe/" +
            "Fault/InvalidRequestMessageFaultType", Name = "InvalidRequestMessageFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://tempuri.org/INotificationService/Unsubscribe/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/INotificationService/Unsubscribe", ReplyAction = "http://tempuri.org/INotificationService/UnsubscribeResponse")]
        void Unsubscribe(EndpointReferenceType job);
    }
}
