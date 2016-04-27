﻿using System.Collections.Generic;

using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core
{
    public delegate MapSpecification<IQuery, IEnumerable<TOutput>> MapToObjectsSpecProvider<TFilter, TOutput>(FindSpecification<TFilter> specification);
}