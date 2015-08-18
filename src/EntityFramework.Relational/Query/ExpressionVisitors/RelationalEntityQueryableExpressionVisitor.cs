// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Query.Annotations;
using Microsoft.Data.Entity.Query.Expressions;
using Microsoft.Data.Entity.Query.Sql;
using Microsoft.Data.Entity.Relational.Internal;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace Microsoft.Data.Entity.Query.ExpressionVisitors
{
    public class RelationalEntityQueryableExpressionVisitor : EntityQueryableExpressionVisitor
    {
        private readonly IModel _model;
        private readonly IEntityKeyFactorySource _entityKeyFactorySource;
        private readonly IMaterializerFactory _materializerFactory;
        private readonly ISqlQueryGeneratorFactory _sqlQueryGeneratorFactory;
        private readonly ICommandBuilderFactory _commandBuilderFactory;
        private readonly IRelationalMetadataExtensionProvider _relationalMetadataExtensionProvider;

        private static readonly ParameterExpression _valueBufferParameter
            = Expression.Parameter(typeof(ValueBuffer));

        private IQuerySource _querySource;

        public virtual IQuerySource QuerySource
        {
            get { return _querySource; }
            [param: NotNull]
            set
            {
                Check.NotNull(value, nameof(value));

                _querySource = value;
            }
        }

        public RelationalEntityQueryableExpressionVisitor(
            [NotNull] IModel model,
            [NotNull] IEntityKeyFactorySource entityKeyFactorySource,
            [NotNull] IMaterializerFactory materializerFactory,
            [NotNull] ISqlQueryGeneratorFactory sqlQueryGeneratorFactory,
            [NotNull] ICommandBuilderFactory commandBuilderFactory,
            [NotNull] IRelationalMetadataExtensionProvider relationalMetadataExtensionProvider)
        {
            Check.NotNull(model, nameof(model));
            Check.NotNull(entityKeyFactorySource, nameof(entityKeyFactorySource));
            Check.NotNull(materializerFactory, nameof(materializerFactory));
            Check.NotNull(sqlQueryGeneratorFactory, nameof(sqlQueryGeneratorFactory));
            Check.NotNull(commandBuilderFactory, nameof(commandBuilderFactory));
            Check.NotNull(relationalMetadataExtensionProvider, nameof(relationalMetadataExtensionProvider));

            _model = model;
            _entityKeyFactorySource = entityKeyFactorySource;
            _materializerFactory = materializerFactory;
            _sqlQueryGeneratorFactory = sqlQueryGeneratorFactory;
            _commandBuilderFactory = commandBuilderFactory;
            _relationalMetadataExtensionProvider = relationalMetadataExtensionProvider;
        }

        public virtual new RelationalQueryModelVisitor QueryModelVisitor
        {
            get { return (RelationalQueryModelVisitor)base.QueryModelVisitor; }
            [param: NotNull]
            set
            {
                Check.NotNull(value, nameof(value));

                base.QueryModelVisitor = value;
            }
        }

        protected override Expression VisitSubQuery(SubQueryExpression subQueryExpression)
        {
            Check.NotNull(subQueryExpression, nameof(subQueryExpression));

            var queryModelVisitor = (RelationalQueryModelVisitor)CreateQueryModelVisitor();

            queryModelVisitor.VisitQueryModel(subQueryExpression.QueryModel);

            if (queryModelVisitor.Queries.Count == 1
                && !queryModelVisitor.RequiresClientFilter
                && !queryModelVisitor.RequiresClientSelectMany
                && !queryModelVisitor.RequiresClientProjection
                && !queryModelVisitor.RequiresClientResultOperator)
            {
                QueryModelVisitor.AddSubquery(_querySource, queryModelVisitor.Queries.First());
            }

            QueryModelVisitor.RegisterSubQueryVisitor(_querySource, queryModelVisitor);

            return queryModelVisitor.Expression;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            Check.NotNull(memberExpression, nameof(memberExpression));

            QueryModelVisitor
                .BindMemberExpression(
                    memberExpression,
                    (property, querySource, selectExpression)
                        => selectExpression.AddToProjection(
                            _relationalMetadataExtensionProvider.For(property).ColumnName,
                            property,
                            querySource),
                    bindSubQueries: true);

            return base.VisitMember(memberExpression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            Check.NotNull(methodCallExpression, nameof(methodCallExpression));

            QueryModelVisitor
                .BindMethodCallExpression(
                    methodCallExpression,
                    (property, querySource, selectExpression)
                        => selectExpression.AddToProjection(
                            _relationalMetadataExtensionProvider.For(property).ColumnName,
                            property,
                            querySource),
                    bindSubQueries: true);

            return base.VisitMethodCall(methodCallExpression);
        }

        protected override Expression VisitEntityQueryable(Type elementType)
        {
            Check.NotNull(elementType, nameof(elementType));

            var queryMethodInfo = CreateValueBufferMethodInfo;
            var relationalQueryCompilationContext = QueryModelVisitor.QueryCompilationContext;
            var entityType = _model.GetEntityType(elementType);
            var selectExpression = new SelectExpression();
            var name = _relationalMetadataExtensionProvider.For(entityType).TableName;

            var tableAlias
                = _querySource.HasGeneratedItemName()
                    ? name[0].ToString().ToLower()
                    : _querySource.ItemName;

            var fromSqlAnnotation
                = relationalQueryCompilationContext
                    .GetCustomQueryAnnotations(RelationalQueryableExtensions.FromSqlMethodInfo)
                    .LastOrDefault(a => a.QuerySource == _querySource);

            var composable = true;
            var sqlString = "";
            object[] sqlParameters = null;

            if (fromSqlAnnotation == null)
            {
                selectExpression.AddTable(
                    new TableExpression(
                        name,
                        _relationalMetadataExtensionProvider.For(entityType).Schema,
                        tableAlias,
                        _querySource));
            }
            else
            {
                sqlString = (string)fromSqlAnnotation.Arguments[1];
                sqlParameters = (object[])fromSqlAnnotation.Arguments[2];

                selectExpression.AddTable(
                    new RawSqlDerivedTableExpression(
                        sqlString,
                        sqlParameters,
                        tableAlias,
                        _querySource));

                var sqlStart = sqlString.SkipWhile(char.IsWhiteSpace).Take(7).ToArray();

                if (sqlStart.Length != 7
                    || !char.IsWhiteSpace(sqlStart.Last())
                    || !new string(sqlStart).StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    if (relationalQueryCompilationContext.QueryAnnotations
                        .OfType<IncludeQueryAnnotation>().Any())
                    {
                        throw new InvalidOperationException(Strings.StoredProcedureIncludeNotSupported);
                    }

                    QueryModelVisitor.RequiresClientEval = true;

                    composable = false;
                }

                if (!fromSqlAnnotation.QueryModel.BodyClauses.Any()
                    && !fromSqlAnnotation.QueryModel.ResultOperators.Any())
                {
                    composable = false;
                }
            }

            QueryModelVisitor.AddQuery(_querySource, selectExpression);

            var queryMethodArguments
                = new List<Expression>
                {
                    Expression.Constant(_querySource),
                    EntityQueryModelVisitor.QueryContextParameter,
                    EntityQueryModelVisitor.QueryResultScopeParameter,
                    _valueBufferParameter,
                    Expression.Constant(0)
                };

            if (QueryModelVisitor.QueryCompilationContext
                .QuerySourceRequiresMaterialization(_querySource)
                || QueryModelVisitor.RequiresClientEval)
            {
                var materializer
                    = _materializerFactory
                        .CreateMaterializer(
                            entityType,
                            selectExpression,
                            (p, se) =>
                                se.AddToProjection(
                                    _relationalMetadataExtensionProvider.For(p).ColumnName,
                                    p,
                                    _querySource),
                            _querySource);

                queryMethodInfo
                    = CreateEntityMethodInfo.MakeGenericMethod(elementType);

                var keyFactory
                    = _entityKeyFactorySource
                        .GetKeyFactory(entityType.GetPrimaryKey());

                queryMethodArguments.AddRange(
                    new[]
                    {
                        Expression.Constant(entityType),
                        Expression.Constant(QueryModelVisitor.QuerySourceRequiresTracking(_querySource)),
                        Expression.Constant(keyFactory),
                        Expression.Constant(entityType.GetPrimaryKey().Properties),
                        materializer
                    });
            }

            Func<ISqlQueryGenerator> sqlQueryGeneratorFunc;

            if (composable)
            {
                sqlQueryGeneratorFunc = () =>
                    _sqlQueryGeneratorFactory.CreateGenerator(selectExpression);
            }
            else
            {
                sqlQueryGeneratorFunc = () =>
                    _sqlQueryGeneratorFactory
                        .CreateRawCommandGenerator(
                            selectExpression,
                            sqlString,
                            sqlParameters);
            }

            return Expression.Call(
                relationalQueryCompilationContext.QueryMethodProvider.ShapedQueryMethod
                    .MakeGenericMethod(queryMethodInfo.ReturnType),
                EntityQueryModelVisitor.QueryContextParameter,
                Expression.Constant(
                    _commandBuilderFactory.Create(sqlQueryGeneratorFunc)),
                Expression.Lambda(
                    Expression.Call(queryMethodInfo, queryMethodArguments),
                    _valueBufferParameter));
        }

        public static readonly MethodInfo CreateValueBufferMethodInfo
            = typeof(RelationalEntityQueryableExpressionVisitor).GetTypeInfo()
                .GetDeclaredMethod(nameof(CreateValueBuffer));

        [UsedImplicitly]
        private static QueryResultScope<ValueBuffer> CreateValueBuffer(
            IQuerySource querySource,
            QueryContext queryContext,
            QueryResultScope parentQueryResultScope,
            ValueBuffer valueBuffer,
            int valueBufferOffset)
        {
            return new QueryResultScope<ValueBuffer>(
                querySource,
                valueBuffer.WithOffset(valueBufferOffset),
                parentQueryResultScope);
        }

        public static readonly MethodInfo CreateEntityMethodInfo
            = typeof(RelationalEntityQueryableExpressionVisitor).GetTypeInfo()
                .GetDeclaredMethod(nameof(CreateEntity));

        [UsedImplicitly]
        private static QueryResultScope<TEntity> CreateEntity<TEntity>(
            IQuerySource querySource,
            QueryContext queryContext,
            QueryResultScope parentQueryResultScope,
            ValueBuffer valueBuffer,
            int valueBufferOffset,
            IEntityType entityType,
            bool queryStateManager,
            EntityKeyFactory entityKeyFactory,
            IReadOnlyList<IProperty> keyProperties,
            Func<ValueBuffer, object> materializer)
            where TEntity : class
        {
            valueBuffer = valueBuffer.WithOffset(valueBufferOffset);

            var entityKey
                = entityKeyFactory.Create(keyProperties, valueBuffer);

            return new QueryResultScope<TEntity>(
                querySource,
                entityKey != EntityKey.InvalidEntityKey
                    ? (TEntity)queryContext.QueryBuffer
                        .GetEntity(
                            entityType,
                            entityKey,
                            new EntityLoadInfo(
                                valueBuffer,
                                materializer),
                            queryStateManager)
                    : null,
                parentQueryResultScope);
        }
    }
}
