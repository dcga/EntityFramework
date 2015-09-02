// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Query;
using Microsoft.Data.Entity.Query.ExpressionTranslators;
using Microsoft.Data.Entity.Update;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;
using Remotion.Linq;

namespace Microsoft.Data.Entity.Storage
{
    public abstract class RelationalDatabase : Database
    {
        private readonly ICommandBatchPreparer _batchPreparer;
        private readonly IBatchExecutor _batchExecutor;
        private readonly IRelationalConnection _connection;
        private readonly IDbContextOptions _options;
        private readonly IMethodCallTranslator _compositeMethodCallTranslator;
        private readonly IMemberTranslator _compositeMemberTranslator;
        private readonly IExpressionFragmentTranslator _compositeExpressionFragmentTranslator;

        protected RelationalDatabase(
            [NotNull] IModel model,
            [NotNull] IEntityKeyFactorySource entityKeyFactorySource,
            [NotNull] IEntityMaterializerSource entityMaterializerSource,
            [NotNull] IClrAccessorSource<IClrPropertyGetter> clrPropertyGetterSource,
            [NotNull] IRelationalConnection connection,
            [NotNull] ICommandBatchPreparer batchPreparer,
            [NotNull] IBatchExecutor batchExecutor,
            [NotNull] IDbContextOptions options,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IRelationalValueBufferFactoryFactory valueBufferFactoryFactory,
            [NotNull] IMethodCallTranslator compositeMethodCallTranslator,
            [NotNull] IMemberTranslator compositeMemberTranslator,
            [NotNull] IExpressionFragmentTranslator compositeExpressionFragmentTranslator,
            [NotNull] IRelationalTypeMapper typeMapper,
            [NotNull] IRelationalMetadataExtensionProvider relationalExtensions)
            : base(model, loggerFactory)
        {
            Check.NotNull(entityKeyFactorySource, nameof(entityKeyFactorySource));
            Check.NotNull(entityMaterializerSource, nameof(entityMaterializerSource));
            Check.NotNull(clrPropertyGetterSource, nameof(clrPropertyGetterSource));
            Check.NotNull(connection, nameof(connection));
            Check.NotNull(batchPreparer, nameof(batchPreparer));
            Check.NotNull(batchExecutor, nameof(batchExecutor));
            Check.NotNull(options, nameof(options));
            Check.NotNull(valueBufferFactoryFactory, nameof(valueBufferFactoryFactory));
            Check.NotNull(compositeMethodCallTranslator, nameof(compositeMethodCallTranslator));
            Check.NotNull(compositeMemberTranslator, nameof(compositeMemberTranslator));
            Check.NotNull(compositeExpressionFragmentTranslator, nameof(compositeExpressionFragmentTranslator));
            Check.NotNull(typeMapper, nameof(typeMapper));
            Check.NotNull(relationalExtensions, nameof(relationalExtensions));

            EntityKeyFactorySource = entityKeyFactorySource;
            EntityMaterializerSource = entityMaterializerSource;
            ClrPropertyGetterSource = clrPropertyGetterSource;

            _batchPreparer = batchPreparer;
            _batchExecutor = batchExecutor;
            _connection = connection;
            _options = options;
            _compositeMethodCallTranslator = compositeMethodCallTranslator;
            _compositeMemberTranslator = compositeMemberTranslator;
            _compositeExpressionFragmentTranslator = compositeExpressionFragmentTranslator;

            TypeMapper = typeMapper;
            ValueBufferFactoryFactory = valueBufferFactoryFactory;
            RelationalExtensions = relationalExtensions;
        }

        protected virtual IEntityKeyFactorySource EntityKeyFactorySource { get; }

        protected virtual IEntityMaterializerSource EntityMaterializerSource { get; }

        protected virtual IClrAccessorSource<IClrPropertyGetter> ClrPropertyGetterSource { get; }

        protected virtual IRelationalTypeMapper TypeMapper { get; }

        protected virtual IRelationalMetadataExtensionProvider RelationalExtensions { get; }

        protected virtual IRelationalValueBufferFactoryFactory ValueBufferFactoryFactory { get; }

        protected virtual IDbContextOptions DbContextOptions => _options;

        public override int SaveChanges(
            IReadOnlyList<InternalEntityEntry> entries)
            => _batchExecutor.Execute(
                _batchPreparer.BatchCommands(
                    Check.NotNull(entries, nameof(entries)), _options),
                _connection);

        public override Task<int> SaveChangesAsync(
            IReadOnlyList<InternalEntityEntry> entries,
            CancellationToken cancellationToken = default(CancellationToken))
            => _batchExecutor.ExecuteAsync(
                _batchPreparer.BatchCommands(
                    Check.NotNull(entries, nameof(entries)), _options),
                _connection,
                cancellationToken);

        public override Func<QueryContext, IEnumerable<TResult>> CompileQuery<TResult>(QueryModel queryModel)
            => CreateQueryCompilationContext(
                new LinqOperatorProvider(),
                new RelationalResultOperatorHandler(),
                new QueryMethodProvider(),
                _compositeMethodCallTranslator,
                _compositeMemberTranslator,
                _compositeExpressionFragmentTranslator)
                .CreateQueryModelVisitor()
                .CreateQueryExecutor<TResult>(
                    Check.NotNull(queryModel, nameof(queryModel)));

        public override Func<QueryContext, IAsyncEnumerable<TResult>> CompileAsyncQuery<TResult>(QueryModel queryModel)
            => CreateQueryCompilationContext(
                new AsyncLinqOperatorProvider(),
                new RelationalResultOperatorHandler(),
                new AsyncQueryMethodProvider(),
                _compositeMethodCallTranslator,
                _compositeMemberTranslator,
                _compositeExpressionFragmentTranslator)
                .CreateQueryModelVisitor()
                .CreateAsyncQueryExecutor<TResult>(
                    Check.NotNull(queryModel, nameof(queryModel)));

        protected abstract RelationalQueryCompilationContext CreateQueryCompilationContext(
            [NotNull] ILinqOperatorProvider linqOperatorProvider,
            [NotNull] IResultOperatorHandler resultOperatorHandler,
            [NotNull] IQueryMethodProvider queryMethodProvider,
            [NotNull] IMethodCallTranslator compositeMethodCallTranslator,
            [NotNull] IMemberTranslator compositeMemberTranslator,
            [NotNull] IExpressionFragmentTranslator compositeExpressionFragmentTranslator);
    }
}
