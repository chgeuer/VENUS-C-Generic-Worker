//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿[assembly: WebActivator.PreApplicationStartMethod(typeof(ManagementWeb.AppStart_MefContribMVC3), "Start")]

namespace ManagementWeb
{
    using System.ComponentModel.Composition.Hosting;
    using System.Linq;
    using System.Web.Mvc;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
    using Microsoft.EMIC.Cloud.Storage.Azure;
    using MefContrib.Hosting.Conventions;
    using MefContrib.Web.Mvc;

    public static class AppStart_MefContribMVC3
    {
        private static CompositionContainer m_CompositionContainer;
        private static object m_lock = new object();
        public static CompositionContainer Container
        {
            get
            {
                if (m_CompositionContainer == null)
                {
                    lock (m_lock)
                    {
                        if (m_CompositionContainer == null)
                        {
                            m_CompositionContainer = new CompositionContainer(new AggregateCatalog(
                                    new AssemblyCatalog(typeof(BESFactoryPortTypeImplService).Assembly),
                                    new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                                    new AssemblyCatalog(typeof(AzureArgumentSingleReference).Assembly),
                                    new TypeCatalog(typeof(OnPremisesSettings.OnPremisesSettings))
                                 ));
                        }
                    }
                }

                return m_CompositionContainer;
            }
        }

        public static void Start()
        {
            // Create MEF catalog based on the contents of ~/bin.
            //
            // Note that any class in the referenced assemblies implementing in "IController"
            // is automatically exported to MEF. There is no need for explicit [Export] attributes
            // on ASP.NET MVC controllers. When implementing multiple constructors ensure that
            // there is one constructor marked with the [ImportingConstructor] attribute.

            var catalog = new AggregateCatalog(
                new DirectoryCatalog("bin"),
                new ConventionCatalog(new MvcApplicationRegistry())
               );

            // Tell MVC3 to use MEF as its dependency resolver.
            var dependencyResolver = new CompositionDependencyResolver(catalog);
            DependencyResolver.SetResolver(dependencyResolver);

            // Tell MVC3 to resolve dependencies in controllers
            ControllerBuilder.Current.SetControllerFactory(
                new DefaultControllerFactory(
                    new CompositionControllerActivator(dependencyResolver)));

            // Tell MVC3 to resolve dependencies in filters
            FilterProviders.Providers.Remove(FilterProviders.Providers.Single(f => f is FilterAttributeFilterProvider));
            FilterProviders.Providers.Add(new CompositionFilterAttributeFilterProvider(dependencyResolver));

            // Tell MVC3 to resolve dependencies in model validators
            ModelValidatorProviders.Providers.Remove(ModelValidatorProviders.Providers.OfType<DataAnnotationsModelValidatorProvider>().Single());
            ModelValidatorProviders.Providers.Add(
                new CompositionDataAnnotationsModelValidatorProvider(dependencyResolver));

            // Tell MVC3 to resolve model binders through MEF. Note that a model binder should be decorated
            // with [ModelBinderExport].
            ModelBinderProviders.BinderProviders.Add(
                new CompositionModelBinderProvider(dependencyResolver));
        }
    }
}