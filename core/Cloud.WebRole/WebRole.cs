//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using Microsoft.EMIC.Cloud.ApplicationRepository;

namespace Cloud.WebRole
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.ServiceModel;
    using System.Text;
    using System.Threading;
    using Microsoft.EMIC.Cloud.AzureSettings;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
    using Microsoft.EMIC.Cloud.Storage.Azure;
    using Microsoft.EMIC.Cloud.UserAdministration;
    using Microsoft.EMIC.Cloud.Utilities;
    using Microsoft.EMIC.DevelopmentFabric.Utils;
    using Microsoft.Web.Administration;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Microsoft.EMIC.Cloud;
    using Microsoft.EMIC.Cloud.Security;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;
    using Microsoft.Win32;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    public class WebRole : RoleEntryPoint
    {
        const string DiagnosticsConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        private GenericWorkerDriver Driver { get; set; }
        private CancellationTokenSource _cts;
        private List<LocalJobSubmissionHost> _localSubmissionHostHosts;
        private CompositionContainer container;
        

        public WebRole()
        {
            container = this.GWContainer;
        }

        private CompositionContainer GWContainer
        {
            get
            {
               return new CompositionContainer(new AggregateCatalog(
               new TypeCatalog(
                   typeof(AzureSettingsProvider),
                   typeof(WebRoleSettings)
                    //, typeof(SelfHostedAppStoreServiceImpl)
                   ),
               new AssemblyCatalog(typeof(FilesystemMapper).Assembly),
               new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
               new AssemblyCatalog(typeof(AzureArgumentSingleReference).Assembly),
               new AssemblyCatalog(typeof(KTH.GenericWorker.CDMI.CDMIBlobReference).Assembly),
               new AssemblyCatalog(typeof(AzureQueueJobStatusNotificationPlugin).Assembly)
               ));
            }
        }
        public override void Run()
        {
            Trace.WriteLine("Generic Worker WebRole entry point Run() called", "Information");
            var rt = container.GetExportedValue<IGWRuntimeEnvironment>();

            var hygieneTask = Hygiene.Start(rt, _cts.Token);

            try
            {                
                var numThreads = container.GetExportedValue<int>(CompositionIdentifiers.GenericWorkerParallelTasks);
                for (int i = 1; i < numThreads; i++)
                {
                    var cnt = this.GWContainer;                    
                    var driver = cnt.GetExportedValue<GenericWorkerDriver>();
                    Trace.WriteLine("Opening Endpoint: "+ driver.LocalJobSubmissionEndPoint, "Information");
                    var localSubmissionHostHost = new LocalJobSubmissionHost(cnt, driver.LocalJobSubmissionEndPoint);
                    localSubmissionHostHost.Open();
                    this._localSubmissionHostHosts.Add(localSubmissionHostHost);
                    Task.Factory.StartNew(() => driver.Run(_cts.Token), _cts.Token);    
                }

                this.Driver.Run(_cts.Token); 
            }
            catch (Exception exception)
            {
                var errorMsg = new StringBuilder();
                for (var e = exception; e != null; e = e.InnerException)
                {
                    errorMsg.AppendLine(
                        string.Format("{0}: \"{1}\"",
                            e.GetType().Name, e.Message));
                }
                Trace.TraceError(errorMsg.ToString());

                _cts.Cancel();

                throw;
            }            
        }

        public override bool OnStart()
        {
            Trace.TraceInformation("OnStart() called");
            MicrosoftCorpnetAuthenticationFixer.TweakIdentityWhenRunningInCorpnet();

            TimeSpan scheduledTransferPeriod = TimeSpan.FromMinutes(1);
            ServicePointManager.DefaultConnectionLimit = 12;
            DiagnosticMonitorConfiguration diagConfig = DiagnosticMonitor.GetDefaultInitialConfiguration();
            diagConfig.DiagnosticInfrastructureLogs.ScheduledTransferLogLevelFilter = LogLevel.Information;
            diagConfig.DiagnosticInfrastructureLogs.ScheduledTransferPeriod = scheduledTransferPeriod;
            diagConfig.WindowsEventLog.ScheduledTransferLogLevelFilter = LogLevel.Warning;
            diagConfig.WindowsEventLog.ScheduledTransferPeriod = scheduledTransferPeriod;
            diagConfig.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
            diagConfig.Logs.ScheduledTransferPeriod = scheduledTransferPeriod;

            diagConfig.Directories.DataSources.Add(new DirectoryConfiguration
            {
                Path = new DirectoryInfo(@"SetupScripts\setuplogs").FullName,
                Container = "setupscriptlogs",
                DirectoryQuotaInMB = 100
            });
            diagConfig.Directories.ScheduledTransferPeriod = scheduledTransferPeriod;

            diagConfig.ConfigurationChangePollInterval = TimeSpan.FromMinutes(1);
            DiagnosticMonitor.Start(DiagnosticsConnectionString, diagConfig);

            // For STS and / or self issued tokens, we need to trust the certificate.
            // To be sure we also trust self signed certificates, it needs to be copied to
            // TrustedPeople and CA store.
            var STSCertificateThumbprint = container.GetExportedValue<string>(CompositionIdentifiers.STSCertificateThumbprint);
            var stsCert = X509Helper.GetX509Certificate2(StoreLocation.LocalMachine, StoreName.My, STSCertificateThumbprint, X509FindType.FindByThumbprint);
            if (stsCert != null)
            {
                var trustedRootCAStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                var trustedPeopleStore = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
                try
                {
                    trustedRootCAStore.Open(OpenFlags.ReadWrite);
                    trustedPeopleStore.Open(OpenFlags.ReadWrite);

                    trustedRootCAStore.Add(stsCert);
                    trustedPeopleStore.Add(stsCert);

                    trustedRootCAStore.Close();
                    trustedPeopleStore.Close();
                }
                catch (Exception)
                {
                    Trace.TraceInformation("OnStart(): Certificate stores couldn't be opened for copying STS certificate.");
                }
            }
            else
            {
                Trace.TraceInformation("OnStart(): STS certificate couldn't be found.");
            }

            // It is possible to install e.g. runtimes by using a user setup script. This script is executed before
            // the OnStart-Method is called. If the setup script changes environment variables, these changes are not
            // recognized by the process / service starting the WebRole. The problem is partly described here:
            // http://support.microsoft.com/kb/887693/en-us.
            // As a workaround, the most important system environment variable "Path" can be retrieved by reading
            // the registry and then adding to the current environment.
            try
            {
                var reg = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Session Manager\\Environment");
                if (reg != null)
                {
                    string registryPath = reg.GetValue("Path").ToString();
                    if (!String.IsNullOrWhiteSpace(registryPath))
                    {
                        string existingPath = Environment.GetEnvironmentVariable(GenericWorkerDriver.ENVIRONMENT_VARIABLE_PATH);
                        Environment.SetEnvironmentVariable(GenericWorkerDriver.ENVIRONMENT_VARIABLE_PATH, String.Format("{0};{1}", existingPath, registryPath));
                        
                        Trace.TraceInformation("OnStart(): Environment variable 'Path' has been updated with value from registry");
                    }
                    else
                    {
                        Trace.TraceInformation("OnStart(): Environment variable 'Path' cannot be found in registry");
                    }
                }
                else
                {
                    Trace.TraceInformation("OnStart(): Registry path for environment variable 'Path' cannot be found.");
                }
            }
            catch (Exception e)
            {
                Trace.TraceInformation("OnStart(): Unexpected error when accessing registry: " + e.Message);
            }
            

            RoleEnvironment.Changing += RoleEnvironmentChanging;
            
            this.Driver = container.GetExportedValue<GenericWorkerDriver>();
            this._cts = new CancellationTokenSource();

            var localJobSummissionEndPoint = this.Driver.LocalJobSubmissionEndPoint;
            var localSubmissionHostHost = new LocalJobSubmissionHost(container, localJobSummissionEndPoint);
            localSubmissionHostHost.Open();

            Trace.WriteLine("Opening Endpoint: " + localJobSummissionEndPoint, "Information");
            this._localSubmissionHostHosts = new List<LocalJobSubmissionHost>();
            this._localSubmissionHostHosts.Add(localSubmissionHostHost);

            WarmUpApplications("HttpPort80");
            //WarmUpApplications("HttpsPort443");

            return base.OnStart();
        }

        public override void OnStop()
        {
            // Shut down the generic worker driver
            this._cts.Cancel();
            foreach (var localSubmissionHost in this._localSubmissionHostHosts)
            {
                localSubmissionHost.Close();    
            }            

            try
            {
                Trace.TraceInformation("Shutting down Profile service. ");
                CreateProfileClient.Shutdown();
            }
            catch (EndpointNotFoundException)
            {
                Trace.TraceInformation("Profile service seems not to run");
            }
            catch (Exception exception)
            {
                var errorMsg = new StringBuilder();
                for (var e = exception; e != null; e = e.InnerException)
                {
                    errorMsg.AppendLine(
                        string.Format("{0}: \"{1}\"",
                            e.GetType().Name, e.Message));
                }
                Trace.TraceError(errorMsg.ToString());
            }

            base.OnStop();
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // If a configuration setting is changing
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                // Set e.Cancel to true to restart this role instance
                e.Cancel = true;
            }
        }

        private static void WarmUpApplications(string endpointName)
        {
            var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[endpointName];

            // TODO: Can we do this parallel
            foreach (var app in AddressUtils.SuffixCollection)
            {
                var address = String.Format("{0}://{1}:{2}{3}",
                    endpoint.Protocol, endpoint.IPEndpoint.Address,
                    endpoint.IPEndpoint.Port, app);

                var error = string.Empty;
                try
                {
                    new WebClient().DownloadString(address);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
 
                if (string.IsNullOrEmpty(error))
                    Trace.TraceInformation(string.Format("Warm-up of '{0}' was successful", address));
                else 
                    Trace.TraceError(string.Format("Warm-up of '{0}' failed: '{1}'", address, error));
            }
        }
    }
}