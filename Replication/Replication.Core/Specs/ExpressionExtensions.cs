using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.Specs
{
    public static class ExpressionExtensions
    {
        private const int MsSqlInExpressionLimit = 5000;

        public static FindSpecificationCollection<T> In<T, TKey>(
            this Expression<Func<T, TKey>> keyProperyExpression,
            IReadOnlyCollection<TKey> keys)
        {
            return In(keyProperyExpression, keys, MsSqlInExpressionLimit);
        }

        public static FindSpecificationCollection<T> In<T, TKey>(
            this Expression<Func<T, TKey>> keyProperyExpression,
            IReadOnlyCollection<TKey> keys,
            int limit)
        {
            Func<IEnumerable<TKey>, TKey, bool> contains = Enumerable.Contains;
            var specs = Enumerable.Range(0, (limit + keys.Count - 1) / limit)
                                  .Select(
                                      x =>
                                      {
                                          var batch = keys.Skip(x * limit).Take(limit);

                                          // x => batch.Contains(x.KeyProperty)
                                          var containsCall = Expression.Call(
                                              null,
                                              contains.Method,
                                              Expression.Constant(batch),
                                              keyProperyExpression.Body);
                                          return new FindSpecification<T>(Expression.Lambda<Func<T, bool>>(containsCall, keyProperyExpression.Parameters[0]));
                                      })
                                  .ToArray();
            return new FindSpecificationCollection<T>(specs);
        }
    }
}