// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Data.Entity.Query.ExpressionVisitors;

namespace Microsoft.Data.Entity.Query
{
    public class InMemoryQueryCompilationContext : QueryCompilationContext
    {
        private readonly IServiceProvider _serviceProvider;

        public InMemoryQueryCompilationContext(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] ISyncAsyncServices syncAsyncServices,
            [NotNull] IEntityQueryModelVisitorFactory entityQueryModelVisitorFactory,
            [NotNull] IRequiresMaterializationExpressionVisitorFactory requiresMaterializationExpressionVisitorFactory,
            [NotNull] IServiceProvider serviceProvider)
            : base(
                Check.NotNull(loggerFactory, nameof(loggerFactory)),
                Check.NotNull(syncAsyncServices, nameof(syncAsyncServices)),
                Check.NotNull(entityQueryModelVisitorFactory, nameof(entityQueryModelVisitorFactory)),
                Check.NotNull(requiresMaterializationExpressionVisitorFactory, nameof(requiresMaterializationExpressionVisitorFactory)))
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
