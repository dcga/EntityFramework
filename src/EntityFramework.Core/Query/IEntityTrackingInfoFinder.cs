﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Microsoft.Data.Entity.Query
{
    public interface IEntityTrackingInfoFinder
    {
        IReadOnlyCollection<EntityTrackingInfo> Find(
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] Expression expression);
    }
}
