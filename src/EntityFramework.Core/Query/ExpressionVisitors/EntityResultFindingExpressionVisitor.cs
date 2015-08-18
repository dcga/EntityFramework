// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace Microsoft.Data.Entity.Query.ExpressionVisitors
{
    public class EntityResultFindingExpressionVisitor : ExpressionVisitorBase, IEntityResultFindingExpressionVisitor
    {
        private readonly IModel _model;
        private readonly IEntityTrackingInfoFactory _entityTrackingInfoFactory;

        private QueryCompilationContext _queryCompilationContext;
        private ISet<IQuerySource> _untrackedQuerySources;

        private List<EntityTrackingInfo> _entityTrackingInfos;

        public EntityResultFindingExpressionVisitor(
            [NotNull] IModel model,
            [NotNull] IEntityTrackingInfoFactory entityTrackingInfoFactory)
        {
            Check.NotNull(model, nameof(model));
            Check.NotNull(entityTrackingInfoFactory, nameof(entityTrackingInfoFactory));

            _model = model;
            _entityTrackingInfoFactory = entityTrackingInfoFactory;
        }

        public virtual IReadOnlyCollection<EntityTrackingInfo> Find(
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] Expression expression)
        {
            Check.NotNull(queryCompilationContext, nameof(queryCompilationContext));
            Check.NotNull(expression, nameof(expression));

            _queryCompilationContext = queryCompilationContext;

            _untrackedQuerySources
                = new HashSet<IQuerySource>(
                    _queryCompilationContext
                        .GetCustomQueryAnnotations(EntityFrameworkQueryableExtensions.AsNoTrackingMethodInfo)
                        .Select(qa => qa.QuerySource));

            _entityTrackingInfos = new List<EntityTrackingInfo>();

            Visit(expression);

            return _entityTrackingInfos;
        }

        protected override Expression VisitQuerySourceReference(
            QuerySourceReferenceExpression querySourceReferenceExpression)
        {
            if (!_untrackedQuerySources.Contains(querySourceReferenceExpression.ReferencedQuerySource))
            {
                var entityType = _model.FindEntityType(querySourceReferenceExpression.Type);

                if (entityType != null)
                {
                    _entityTrackingInfos.Add(
                        _entityTrackingInfoFactory
                            .Create(_queryCompilationContext, querySourceReferenceExpression, entityType));
                }
            }

            return querySourceReferenceExpression;
        }

        // Prune these nodes...

        protected override Expression VisitSubQuery(SubQueryExpression expression)
        {
            return expression;
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            return expression;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            return expression;
        }

        protected override Expression VisitConditional(ConditionalExpression expression)
        {
            return expression;
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            return expression;
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression expression)
        {
            return expression;
        }

        protected override Expression VisitLambda<T>(Expression<T> expression)
        {
            return expression;
        }

        protected override Expression VisitInvocation(InvocationExpression expression)
        {
            return expression;
        }
    }
}
