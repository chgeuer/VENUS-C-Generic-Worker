//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using OGF.BES.Faults;
namespace OGF.BES.Managememt
{
    /// <summary>
    /// BES Management Interface defined by the OGF
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-management", ConfigurationName = "BESManagementPortType")]
    public interface IBESManagementPortType
    {

        // CODEGEN: Generating message contract since the operation StopAcceptingNewActivities is neither RPC nor document wrapped.
        /// <summary>
        /// Stops accepting new activities specified by the StopAcceptingNewActivitiesRequest input and returns a StopAcceptingNewActivitiesResponse output
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-management/BESManagementPortType/StopAccep" +
            "tingNewActivities", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-management/BESManagementPortType/StopAccep" +
            "tingNewActivitiesResponse")]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-management/BESManagementPortType/StopAccep" +
            "tingNewActivities/Fault/NotAuthorizedFault", Name = "NotAuthorizedFault", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        StopAcceptingNewActivitiesResponse StopAcceptingNewActivities(StopAcceptingNewActivitiesRequest request);

        // CODEGEN: Generating message contract since the operation StartAcceptingNewActivities is neither RPC nor document wrapped.
        /// <summary>
        /// Starts accepting new activities specified by the StartAcceptingNewActivitiesRequest input and returns a StartAcceptingNewActivitiesResponse output
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-management/BESManagementPortType/StartAcce" +
            "ptingNewActivities", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-management/BESManagementPortType/StartAcce" +
            "ptingNewActivitiesResponse")]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-management/BESManagementPortType/StartAcce" +
            "ptingNewActivities/Fault/NotAuthorizedFault", Name = "NotAuthorizedFault", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory")]
        [System.ServiceModel.XmlSerializerFormatAttribute()]
        StartAcceptingNewActivitiesResponse StartAcceptingNewActivities(StartAcceptingNewActivitiesRequest request);
    }
}
