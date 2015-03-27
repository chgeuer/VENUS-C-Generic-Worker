//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





namespace OGF.BES.Managememt
{
    /// <summary>
    /// Wrapper for Stop Accepting New Activities Response
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class StopAcceptingNewActivitiesRequest
    {

        /// <summary>
        /// An instance of <see cref="StopAcceptingNewActivitiesType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-management", Order = 0)]
        public StopAcceptingNewActivitiesType StopAcceptingNewActivities;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopAcceptingNewActivitiesRequest"/> class.
        /// </summary>
        public StopAcceptingNewActivitiesRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StopAcceptingNewActivitiesRequest"/> class.
        /// </summary>
        /// <param name="StopAcceptingNewActivities">The stop accepting new activities.</param>
        public StopAcceptingNewActivitiesRequest(StopAcceptingNewActivitiesType StopAcceptingNewActivities)
        {
            this.StopAcceptingNewActivities = StopAcceptingNewActivities;
        }
    }
}
