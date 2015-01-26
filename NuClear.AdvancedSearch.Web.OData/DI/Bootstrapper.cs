﻿using System.Data.Entity.Infrastructure;
using System.Web.Http.Dispatcher;

using DoubleGis.Erm.Platform.DI.Common.Config;

using Effort.Provider;

using Microsoft.Practices.Unity;

using NuClear.AdvancedSearch.EntityDataModel.Metadata;
using NuClear.AdvancedSearch.Web.OData.Dynamic;
using NuClear.EntityDataModel.EntityFramework.Building;
using NuClear.EntityDataModel.EntityFramework.Emit;
using NuClear.Metamodeling.Processors;
using NuClear.Metamodeling.Provider;
using NuClear.Metamodeling.Provider.Sources;

namespace NuClear.AdvancedSearch.Web.OData.DI
{
    internal static class Bootstrapper
    {
        public static IUnityContainer ConfigureUnity()
        {
            var container = new UnityContainer()
                .ConfigureMetadata()
                .ConfigureStoreModel()
                .ConfigureWebApi();

            return container;
        }

        public static IUnityContainer ConfigureMetadata(this IUnityContainer container)
        {
            var metadataSources = new IMetadataSource[]
            {
                new AdvancedSearchMetadataSource()
            };

            var metadataProcessors = new IMetadataProcessor[] { };

            return container.RegisterType<IMetadataProvider, MetadataProvider>(Lifetime.Singleton, new InjectionConstructor(metadataSources, metadataProcessors));
        }

        public static IUnityContainer ConfigureStoreModel(this IUnityContainer container)
        {
            EffortProviderConfiguration.RegisterProvider();
            var effortProviderInfo = new DbProviderInfo(EffortProviderConfiguration.ProviderInvariantName, EffortProviderManifestTokens.Version1);

            return container
                .RegisterType<ITypeProvider, EmitTypeProvider>(Lifetime.Singleton)
                .RegisterType<EdmxModelBuilder>(Lifetime.Singleton, new InjectionConstructor(effortProviderInfo, typeof(IMetadataProvider), typeof(ITypeProvider)));
        }

        public static IUnityContainer ConfigureWebApi(this IUnityContainer container)
        {
            return container
                .RegisterType<IDynamicAssembliesRegistry, DynamicAssembliesRegistry>(Lifetime.Singleton)
                .RegisterType<IDynamicAssembliesResolver, DynamicAssembliesRegistry>(Lifetime.Singleton)
                .RegisterType<IHttpControllerTypeResolver, DynamicControllerTypeResolver>(Lifetime.Singleton);
        }
    }
}