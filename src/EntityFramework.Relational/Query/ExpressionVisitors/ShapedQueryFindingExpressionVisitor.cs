// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Query.ExpressionVisitors
{
    public class ShapedQueryFindingExpressionVisitor : ExpressionVisitorBase
    {
        private readonly IRelationalSyncAsyncServices _relationalSyncAsyncServices;

        private MethodCallExpression _shapedQueryMethodCall;

        public ShapedQueryFindingExpressionVisitor([NotNull] IRelationalSyncAsyncServices relationalSyncAsyncServices)
        {
            Check.NotNull(relationalSyncAsyncServices, nameof(relationalSyncAsyncServices));

            _relationalSyncAsyncServices = relationalSyncAsyncServices;
        }

        public virtual MethodCallExpression Find([NotNull] Expression expression)
        {
            Check.NotNull(expression, nameof(expression));

            Visit(expression);

            return _shapedQueryMethodCall;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            Check.NotNull(methodCallExpression, nameof(methodCallExpression));

            if (methodCallExpression.Method.MethodIsClosedFormOf(
                _relationalSyncAsyncServices.QueryMethodProvider.ShapedQueryMethod))
            {
                _shapedQueryMethodCall = methodCallExpression;
            }

            return _shapedQueryMethodCall == null
                ? base.VisitMethodCall(methodCallExpression)
                : methodCallExpression;
        }
    }
}
