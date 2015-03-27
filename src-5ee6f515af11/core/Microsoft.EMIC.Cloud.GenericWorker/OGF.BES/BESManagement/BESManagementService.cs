//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





namespace OGF.BES.Managememt
{
    
    /// <summary>
    /// Class implements the BESManagent Interface
    /// </summary>
    [System.ServiceModel.ServiceBehaviorAttribute(InstanceContextMode=System.ServiceModel.InstanceContextMode.PerCall, ConcurrencyMode=System.ServiceModel.ConcurrencyMode.Multiple)]
    public class BESManagementService : IBESManagementPortType
    {
        /// <summary>
        /// Stops accepting new activities specified by the StopAcceptingNewActivitiesRequest input and returns a StopAcceptingNewActivitiesResponse output
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public virtual StopAcceptingNewActivitiesResponse StopAcceptingNewActivities(StopAcceptingNewActivitiesRequest request)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Starts accepting new activities specified by the StartAcceptingNewActivitiesRequest input and returns a StartAcceptingNewActivitiesResponse output
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public virtual StartAcceptingNewActivitiesResponse StartAcceptingNewActivities(StartAcceptingNewActivitiesRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
