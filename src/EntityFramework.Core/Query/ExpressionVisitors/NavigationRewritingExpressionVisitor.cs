﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Microsoft.Data.Entity.Query.ExpressionVisitors
{
    public class NavigationRewritingExpressionVisitor : RelinqExpressionVisitor, INavigationRewritingExpressionVisitor
    {
        private class NavigationJoin
        {
            public NavigationJoin(
                IQuerySource querySource,
                INavigation navigation,
                JoinClause joinClause,
                QuerySourceReferenceExpression querySourceReferenceExpression)
            {
                QuerySource = querySource;
                Navigation = navigation;
                JoinClause = joinClause;
                QuerySourceReferenceExpression = querySourceReferenceExpression;
            }

            public IQuerySource QuerySource { get; }
            public INavigation Navigation { get; }
            public JoinClause JoinClause { get; }
            public QuerySourceReferenceExpression QuerySourceReferenceExpression { get; }
            public readonly List<NavigationJoin> NavigationJoins = new List<NavigationJoin>();

            public IEnumerable<NavigationJoin> Iterate()
            {
                yield return this;

                foreach (var navigationJoin in NavigationJoins.SelectMany(nj => nj.Iterate()))
                {
                    yield return navigationJoin;
                }
            }
        }

        private EntityQueryModelVisitor _queryModelVisitor;
        private List<NavigationJoin> _navigationJoins;
        private IEntityQueryProvider _entityQueryProvider;

        public virtual void Rewrite(
            [NotNull] EntityQueryModelVisitor queryModelVisitor,
            [NotNull] QueryModel queryModel)
        {
            Check.NotNull(queryModelVisitor, nameof(queryModelVisitor));
            Check.NotNull(queryModel, nameof(queryModel));

            _queryModelVisitor = queryModelVisitor;
            _navigationJoins = new List<NavigationJoin>();
            _entityQueryProvider = null;

            queryModel.TransformExpressions(Visit);

            var insertionIndex = 0;

            foreach (var navigationJoin in _navigationJoins)
            {
                var bodyClause = navigationJoin.QuerySource as IBodyClause;

                if (bodyClause != null)
                {
                    insertionIndex = queryModel.BodyClauses.IndexOf(bodyClause) + 1;
                }

                var i = insertionIndex;

                foreach (var nj in navigationJoin.Iterate())
                {
                    queryModel.BodyClauses.Insert(i++, nj.JoinClause);
                }
            }
        }

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            if (_entityQueryProvider == null)
            {
                _entityQueryProvider
                    = (constantExpression.Value as IQueryable)?.Provider as IEntityQueryProvider;
            }

            return constantExpression;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            Check.NotNull(memberExpression, nameof(memberExpression));

            return
                _queryModelVisitor.BindNavigationPathMemberExpression(
                    memberExpression,
                    (ps, qs) =>
                        {
                            var properties = ps.ToList();
                            var navigations = properties.OfType<INavigation>().ToList();

                            if (navigations.Any())
                            {
                                var outerQuerySourceReferenceExpression = new QuerySourceReferenceExpression(qs);

                                var innerQuerySourceReferenceExpression
                                    = CreateJoinsForNavigations(outerQuerySourceReferenceExpression, navigations);

                                return properties.Count == navigations.Count
                                    ? innerQuerySourceReferenceExpression
                                    : Expression.MakeMemberAccess(innerQuerySourceReferenceExpression, memberExpression.Member);
                            }

                            return default(Expression);
                        })
                ?? base.VisitMember(memberExpression);
        }

        private Expression CreateJoinsForNavigations(
            QuerySourceReferenceExpression outerQuerySourceReferenceExpression,
            IEnumerable<INavigation> navigations)
        {
            var querySourceReferenceExpression = outerQuerySourceReferenceExpression;
            var navigationJoins = _navigationJoins;

            foreach (var navigation in navigations)
            {
                var navigationJoin
                    = navigationJoins
                        .FirstOrDefault(nj =>
                            nj.QuerySource == querySourceReferenceExpression.ReferencedQuerySource
                            && nj.Navigation == navigation);

                if (navigationJoin == null)
                {
                    var targetEntityType = navigation.GetTargetType();

                    var joinClause
                        = new JoinClause(
                            $"{querySourceReferenceExpression.ReferencedQuerySource.ItemName}.{navigation.Name}",
                            targetEntityType.ClrType,
                            Expression.Constant(
                                _createEntityQueryableMethod
                                    .MakeGenericMethod(targetEntityType.ClrType)
                                    .Invoke(null, new object[]
                                        {
                                            _entityQueryProvider
                                        })),
                            CreatePropertyCallExpression(
                                querySourceReferenceExpression,
                                navigation.ForeignKey.Properties.Single()),
                            Expression.Constant(null));

                    var innerQuerySourceReferenceExpression
                        = new QuerySourceReferenceExpression(joinClause);

                    Expression innerKeySelector
                        = CreatePropertyCallExpression(
                            innerQuerySourceReferenceExpression,
                            targetEntityType.GetPrimaryKey().Properties.Single());

                    if (innerKeySelector.Type != joinClause.OuterKeySelector.Type)
                    {
                        innerKeySelector
                            = Expression.Convert(
                                innerKeySelector,
                                joinClause.OuterKeySelector.Type);
                    }

                    joinClause.InnerKeySelector = innerKeySelector;

                    navigationJoins.Add(
                        navigationJoin
                            = new NavigationJoin(
                                querySourceReferenceExpression.ReferencedQuerySource,
                                navigation,
                                joinClause,
                                innerQuerySourceReferenceExpression));
                }

                querySourceReferenceExpression = navigationJoin.QuerySourceReferenceExpression;
                navigationJoins = navigationJoin.NavigationJoins;
            }

            return querySourceReferenceExpression;
        }

        private static MethodCallExpression CreatePropertyCallExpression(Expression target, IProperty property)
            => Expression.Call(
                null,
                EntityQueryModelVisitor.PropertyMethodInfo.MakeGenericMethod(property.ClrType),
                target,
                Expression.Constant(property.Name));

        private static readonly MethodInfo _createEntityQueryableMethod
            = typeof(NavigationRewritingExpressionVisitor)
                .GetTypeInfo().GetDeclaredMethod(nameof(_CreateEntityQueryable));

        [UsedImplicitly]
        private static EntityQueryable<TResult> _CreateEntityQueryable<TResult>(IEntityQueryProvider entityQueryProvider)
            => new EntityQueryable<TResult>(entityQueryProvider);
    }
}
