namespace Microsoft.EMIC.Cloud.Security
{
    using System;
    using System.ServiceModel.Configuration;
    
    /// <summary>
    /// Provides a ServiceBehavior for RSA
    /// </summary>
    public class RsaSessionServiceBehaviorExtension : BehaviorExtensionElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RsaSessionServiceBehaviorExtension"/> class.
        /// </summary>
        public RsaSessionServiceBehaviorExtension() { }

        /// <summary>
        /// Gets the type of behavior.
        /// </summary>
        /// <returns>A <see cref="T:System.Type"/>.</returns>
        public override Type BehaviorType
        {
            get { return typeof(RsaSessionServiceBehavior); }
        }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        /// <returns>
        /// The behavior extension.
        /// </returns>
        protected override object CreateBehavior()
        {
            return new RsaSessionServiceBehavior();
        }
    }
}