//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





namespace OGF.BES.Managememt
{
    /// <summary>
    /// Wrapper for Start Accepting New Activities Request
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class StartAcceptingNewActivitiesRequest
    {

        /// <summary>
        /// An instance of <see cref="StartAcceptingNewActivitiesType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-management", Order = 0)]
        public StartAcceptingNewActivitiesType StartAcceptingNewActivities;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartAcceptingNewActivitiesRequest"/> class.
        /// </summary>
        public StartAcceptingNewActivitiesRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartAcceptingNewActivitiesRequest"/> class.
        /// </summary>
        /// <param name="StartAcceptingNewActivities">The start accepting new activities.</param>
        public StartAcceptingNewActivitiesRequest(StartAcceptingNewActivitiesType StartAcceptingNewActivities)
        {
            this.StartAcceptingNewActivities = StartAcceptingNewActivities;
        }
    }
}
