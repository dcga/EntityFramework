// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Query.ExpressionVisitors;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Query
{
    public class QueryCompilationContextServices
    {
        public QueryCompilationContextServices(
            [NotNull] IEntityMaterializerSource entityMaterializerSource,
            [NotNull] IEntityKeyFactorySource entityKeyFactorySource,
            [NotNull] IClrAccessorSource<IClrPropertyGetter> clrPropertyGetterSource,
            [NotNull] IEntityQueryableExpressionVisitorFactory entityQueryableExpressionVisitorFactory,
            [NotNull] IProjectionExpressionVisitorFactory projectionExpressionVisitorFactory,
            [NotNull] IOrderingExpressionVisitorFactory orderingExpressionVisitorFactory,
            [NotNull] IQuerySourceTracingExpressionVisitor querySourceTracingExpressionVisitor,
            [NotNull] IEntityTrackingInfoFactory entityTrackingInfoFactory,
            [NotNull] ITaskBlockingExpressionVisitor taskBlockingExpressionVisitor,
            [NotNull] IMemberAccessBindingExpressionVisitorFactory memberAccessBindingExpressionVisitorFactory,
            [NotNull] IEntityQueryModelVisitorFactory entityQueryModelVisitorFactory,
            [NotNull] IRequiresMaterializationExpressionVisitorFactory requiresMaterializationExpressionVisitorFactory)
        {
            Check.NotNull(entityMaterializerSource, nameof(entityMaterializerSource));
            Check.NotNull(entityKeyFactorySource, nameof(entityKeyFactorySource));
            Check.NotNull(clrPropertyGetterSource, nameof(clrPropertyGetterSource));
            Check.NotNull(entityQueryableExpressionVisitorFactory, nameof(entityQueryableExpressionVisitorFactory));
            Check.NotNull(projectionExpressionVisitorFactory, nameof(projectionExpressionVisitorFactory));
            Check.NotNull(orderingExpressionVisitorFactory, nameof(orderingExpressionVisitorFactory));
            Check.NotNull(querySourceTracingExpressionVisitor, nameof(querySourceTracingExpressionVisitor));
            Check.NotNull(entityTrackingInfoFactory, nameof(entityTrackingInfoFactory));
            Check.NotNull(taskBlockingExpressionVisitor, nameof(taskBlockingExpressionVisitor));
            Check.NotNull(memberAccessBindingExpressionVisitorFactory, nameof(memberAccessBindingExpressionVisitorFactory));
            Check.NotNull(entityQueryModelVisitorFactory, nameof(entityQueryModelVisitorFactory));
            Check.NotNull(requiresMaterializationExpressionVisitorFactory, nameof(requiresMaterializationExpressionVisitorFactory));

            EntityMaterializerSource = entityMaterializerSource;
            EntityKeyFactorySource = entityKeyFactorySource;
            ClrPropertyGetterSource = clrPropertyGetterSource;
            EntityQueryableExpressionVisitorFactory = entityQueryableExpressionVisitorFactory;
            ProjectionExpressionVisitorFactory = projectionExpressionVisitorFactory;
            OrderingExpressionVisitorFactory = orderingExpressionVisitorFactory;
            QuerySourceTracingExpressionVisitor = querySourceTracingExpressionVisitor;
            EntityTrackingInfoFactory = entityTrackingInfoFactory;
            TaskBlockingExpressionVisitor = taskBlockingExpressionVisitor;
            MemberAccessBindingExpressionVisitorFactory = memberAccessBindingExpressionVisitorFactory;
            EntityQueryModelVisitorFactory = entityQueryModelVisitorFactory;
            RequiresMaterializationExpressionVisitorFactory = requiresMaterializationExpressionVisitorFactory;
        }

        public virtual IEntityMaterializerSource EntityMaterializerSource { get; }
        public virtual IEntityKeyFactorySource EntityKeyFactorySource { get; }
        public virtual IClrAccessorSource<IClrPropertyGetter> ClrPropertyGetterSource { get; }
        public virtual IEntityQueryableExpressionVisitorFactory EntityQueryableExpressionVisitorFactory { get; }
        public virtual IProjectionExpressionVisitorFactory ProjectionExpressionVisitorFactory { get; }
        public virtual IOrderingExpressionVisitorFactory OrderingExpressionVisitorFactory { get; }
        public virtual IQuerySourceTracingExpressionVisitor QuerySourceTracingExpressionVisitor { get; }
        public virtual IEntityTrackingInfoFactory EntityTrackingInfoFactory { get; }
        public virtual ITaskBlockingExpressionVisitor TaskBlockingExpressionVisitor { get; }
        public virtual IMemberAccessBindingExpressionVisitorFactory MemberAccessBindingExpressionVisitorFactory { get; }
        public virtual IEntityQueryModelVisitorFactory EntityQueryModelVisitorFactory { get; }
        public virtual IRequiresMaterializationExpressionVisitorFactory RequiresMaterializationExpressionVisitorFactory { get; }
    }
}
