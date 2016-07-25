using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Practices.Unity;

using NuClear.CustomerIntelligence.Replication.Actors;
using NuClear.CustomerIntelligence.Storage.Model.CI;
using NuClear.CustomerIntelligence.Storage.Model.Statistics;
using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;

namespace NuClear.CustomerIntelligence.Replication.Host.Factories.Replication
{
    public sealed class UnityAggregateActorFactory : IAggregateActorFactory
    {
        private static readonly Dictionary<Type, Type> AggregateRootActors =
            new Dictionary<Type, Type>
                {
                    { typeof(Firm), typeof(FirmAggregateRootActor) },
                    { typeof(Client), typeof(ClientAggregateRootActor) },
                    { typeof(Territory), typeof(TerritoryAggregateRootActor) },
                    { typeof(CategoryGroup), typeof(CategoryGroupAggregateRootActor) },
                    { typeof(Project), typeof(ProjectAggregateRootActor) },
                    { typeof(ProjectStatistics), typeof(ProjectStatisticsAggregateRootActor) }
                };

        private readonly IUnityContainer _unityContainer;

        public UnityAggregateActorFactory(IUnityContainer unityContainer)
        {
            _unityContainer = unityContainer;
        }

        public IReadOnlyCollection<IActor> Create(IReadOnlyCollection<Type> aggregateRootTypes)
        {
            var actors = new List<IActor>();
            foreach (var aggregateRootType in AggregateRootActors.Keys)
            {
                if (aggregateRootTypes.Contains(aggregateRootType))
                {
                    var aggregateRootActorType = AggregateRootActors[aggregateRootType];
                    var aggregateRootActor = (IAggregateRootActor)_unityContainer.Resolve(aggregateRootActorType);
                    actors.Add(new AggregateActor(aggregateRootActor));
                }
            }

            return actors;
        }
    }
}