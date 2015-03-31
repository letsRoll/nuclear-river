﻿using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Data.Context;
using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Model.Facts;
using NuClear.AdvancedSearch.Replication.Data;
using NuClear.AdvancedSearch.Replication.Transforming;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming
{
    public sealed class FactsTransformation : BaseTransformation
    {
        private static readonly Dictionary<Type, Func<IFactsContext, IEnumerable<long>, IQueryable>> Queries = 
            new Dictionary<Type, Func<IFactsContext, IEnumerable<long>, IQueryable>>
                {
                    { typeof(Account), (context, ids) => context.Accounts.Where(x => ids.Contains(x.Id)) },
                    { typeof(CategoryFirmAddress), (context, ids) => context.CategoryFirmAddresses.Where(x => ids.Contains(x.Id)) },
                    { typeof(CategoryOrganizationUnit), (context, ids) => context.CategoryOrganizationUnits.Where(x => ids.Contains(x.Id)) },
                    { typeof(Client), (context, ids) => context.Clients.Where(x => ids.Contains(x.Id)) },
                    { typeof(Contact), (context, ids) => context.Contacts.Where(x => ids.Contains(x.Id)) },
                    { typeof(Firm), (context, ids) => context.Firms.Where(x => ids.Contains(x.Id)) },
                    { typeof(FirmAddress), (context, ids) => context.FirmAddresses.Where(x => ids.Contains(x.Id)) },
                    { typeof(FirmContact), (context, ids) => context.FirmContacts.Where(x => ids.Contains(x.Id)) },
                    { typeof(LegalPerson), (context, ids) => context.LegalPersons.Where(x => ids.Contains(x.Id)) },
                    { typeof(Order), (context, ids) => context.Orders.Where(x => ids.Contains(x.Id)) },
                };

        private readonly IFactsContext _source;
        private readonly IFactsContext _target;

        public FactsTransformation(IFactsContext source, IFactsContext target, IDataMapper mapper)
            : base(mapper)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            _source = source;
            _target = target;
        }

        public IEnumerable<OperationInfo> Transform(IEnumerable<OperationInfo> operations)
        {
            var result = Enumerable.Empty<OperationInfo>();

            foreach (var slice in operations.GroupBy(x => new { x.EntityType, x.Operation }))
            {
                var operation = slice.Key.Operation;
                var entityType = slice.Key.EntityType;
                var entityIds = slice.Select(x => x.EntityId).ToArray();

                Func<IFactsContext, IEnumerable<long>, IQueryable> query;
                if (!Queries.TryGetValue(entityType, out query))
                {
                    // exception?
                    continue;
                }

                Load(operation, query(GetOperationContext(operation), entityIds));
            }

            return result;
        }

        private IFactsContext GetOperationContext(Operation operation)
        {
            switch (operation)
            {
                case Operation.Created:
                    return _source;
                case Operation.Updated:
                    return _source;
                case Operation.Deleted:
                    return _target;
                default:
                    throw new ArgumentOutOfRangeException("operation");
            }
        }
    }
}
