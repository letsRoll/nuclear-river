﻿using System.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.Operations;
using NuClear.Messaging.API.Flows;
using NuClear.Messaging.API.Processing.Actors.Accumulators;
using NuClear.OperationsProcessing.Transports.SQLStore.Final;
using NuClear.Replication.OperationsProcessing.Transports;

namespace NuClear.Replication.OperationsProcessing.Final
{
    public sealed class AggregateOperationAccumulator<TMessageFlow> : MessageProcessingContextAccumulatorBase<TMessageFlow, PerformedOperationsFinalProcessingMessage, OperationAggregatableMessage<AggregateOperation>>
        where TMessageFlow : class, IMessageFlow, new()
    {
        protected override OperationAggregatableMessage<AggregateOperation> Process(PerformedOperationsFinalProcessingMessage message)
        {
            var operations = message.FinalProcessings.Select(x => x.OperationId.CreateOperation(x.EntityTypeId, x.EntityId)).ToList();

            return new OperationAggregatableMessage<AggregateOperation>
            {
                TargetFlow = MessageFlow,
                Operations = operations
            };
        }
    }
}