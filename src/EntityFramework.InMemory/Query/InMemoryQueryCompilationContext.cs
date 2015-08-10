// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Data.Entity.Query
{
    public class InMemoryQueryCompilationContext : QueryCompilationContext
    {
        private readonly IServiceProvider _serviceProvider;

        public InMemoryQueryCompilationContext(
            [NotNull] QueryCompilationContextServices services,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IServiceProvider serviceProvider)
            : base(
                Check.NotNull(services, nameof(services)),
                Check.NotNull(loggerFactory, nameof(loggerFactory)))
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
        }

        public override EntityQueryModelVisitor CreateQueryModelVisitor(EntityQueryModelVisitor parentEntityQueryModelVisitor)
        {
            var visitor = _serviceProvider.GetService<InMemoryQueryModelVisitor>();

            visitor.QueryCompilationContext = this;

            return visitor;
        }
    }
}
