// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Query;
using Microsoft.Data.Entity.Query.ExpressionTranslators;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Update;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Moq;
using Xunit;

namespace Microsoft.Data.Entity.Tests
{
    public class RelationalDatabaseTest
    {
        [Fact]
        public async Task SaveChangesAsync_delegates()
        {
            var relationalConnectionMock = new Mock<IRelationalConnection>();
            var commandBatchPreparerMock = new Mock<ICommandBatchPreparer>();
            var batchExecutorMock = new Mock<IBatchExecutor>();
            var valueBufferMock = new Mock<IRelationalValueBufferFactoryFactory>();
            var methodCallTranslatorMock = new Mock<IMethodCallTranslator>();
            var memberTranslatorMock = new Mock<IMemberTranslator>();
            var fragmentTranslatorMock = new Mock<IExpressionFragmentTranslator>();
            var typeMapperMock = new Mock<IRelationalTypeMapper>();
            var relationalExtensionsMock = new Mock<IRelationalMetadataExtensionProvider>();

            var customServices = new ServiceCollection()
                .AddInstance(relationalConnectionMock.Object)
                .AddInstance(commandBatchPreparerMock.Object)
                .AddInstance(batchExecutorMock.Object)
                .AddInstance(valueBufferMock.Object)
                .AddInstance(methodCallTranslatorMock.Object)
                .AddInstance(memberTranslatorMock.Object)
                .AddInstance(fragmentTranslatorMock.Object)
                .AddInstance(typeMapperMock.Object)
                .AddInstance(relationalExtensionsMock.Object)
                .AddScoped<FakeRelationalDatabase>();

            var contextServices = RelationalTestHelpers.Instance.CreateContextServices(customServices);

            var relationalDatabase = contextServices.GetRequiredService<FakeRelationalDatabase>();

            var entries = new List<InternalEntityEntry>();
            var cancellationToken = new CancellationTokenSource().Token;

            await relationalDatabase.SaveChangesAsync(entries, cancellationToken);

            commandBatchPreparerMock.Verify(c => c.BatchCommands(entries, contextServices.GetService<IDbContextOptions>()));
            batchExecutorMock.Verify(be => be.ExecuteAsync(It.IsAny<IEnumerable<ModificationCommandBatch>>(), relationalConnectionMock.Object, cancellationToken));
        }

        [Fact]
        public void SaveChanges_delegates()
        {
            var relationalConnectionMock = new Mock<IRelationalConnection>();
            var commandBatchPreparerMock = new Mock<ICommandBatchPreparer>();
            var batchExecutorMock = new Mock<IBatchExecutor>();
            var valueBufferMock = new Mock<IRelationalValueBufferFactoryFactory>();
            var methodCallTranslatorMock = new Mock<IMethodCallTranslator>();
            var memberTranslatorMock = new Mock<IMemberTranslator>();
            var fragmentTranslatorMock = new Mock<IExpressionFragmentTranslator>();
            var typeMapperMock = new Mock<IRelationalTypeMapper>();
            var relationalExtensionsMock = new Mock<IRelationalMetadataExtensionProvider>();

            var customServices = new ServiceCollection()
                .AddInstance(relationalConnectionMock.Object)
                .AddInstance(commandBatchPreparerMock.Object)
                .AddInstance(batchExecutorMock.Object)
                .AddInstance(valueBufferMock.Object)
                .AddInstance(methodCallTranslatorMock.Object)
                .AddInstance(memberTranslatorMock.Object)
                .AddInstance(fragmentTranslatorMock.Object)
                .AddInstance(typeMapperMock.Object)
                .AddInstance(relationalExtensionsMock.Object)
                .AddScoped<FakeRelationalDatabase>();

            var contextServices = RelationalTestHelpers.Instance.CreateContextServices(customServices);

            var relationalDatabase = contextServices.GetRequiredService<FakeRelationalDatabase>();

            var entries = new List<InternalEntityEntry>();

            relationalDatabase.SaveChanges(entries);

            commandBatchPreparerMock.Verify(c => c.BatchCommands(entries, contextServices.GetService<IDbContextOptions>()));
            batchExecutorMock.Verify(be => be.Execute(It.IsAny<IEnumerable<ModificationCommandBatch>>(), relationalConnectionMock.Object));
        }

        private class FakeRelationalDatabase : RelationalDatabase
        {
            public FakeRelationalDatabase(
                IModel model,
                IEntityKeyFactorySource entityKeyFactorySource,
                IEntityMaterializerSource entityMaterializerSource,
                IClrAccessorSource<IClrPropertyGetter> clrPropertyGetterSource,
                IRelationalConnection connection,
                ICommandBatchPreparer batchPreparer,
                IBatchExecutor batchExecutor,
                IDbContextOptions options,
                ILoggerFactory loggerFactory,
                IRelationalValueBufferFactoryFactory valueBufferFactoryFactory,
                IMethodCallTranslator compositeMethodCallTranslator,
                IMemberTranslator compositeMemberTranslator,
                IExpressionFragmentTranslator compositeExpressionFragmentTranslator,
                IRelationalTypeMapper typeMapper,
                IRelationalMetadataExtensionProvider relationalExtensions)
                : base(
                    model,
                    entityKeyFactorySource,
                    entityMaterializerSource,
                    clrPropertyGetterSource,
                    connection,
                    batchPreparer,
                    batchExecutor,
                    options,
                    loggerFactory,
                    valueBufferFactoryFactory,
                    compositeMethodCallTranslator,
                    compositeMemberTranslator,
                    compositeExpressionFragmentTranslator,
                    typeMapper,
                    relationalExtensions)
            {
            }

            protected override RelationalQueryCompilationContext CreateQueryCompilationContext(
                ILinqOperatorProvider linqOperatorProvider,
                IResultOperatorHandler resultOperatorHandler,
                IQueryMethodProvider queryMethodProvider,
                IMethodCallTranslator compositeMethodCallTranslator,
                IMemberTranslator compositeMemberTranslator,
                IExpressionFragmentTranslator compositeExpressionFragmentTranslator) =>
                    new FakeQueryCompilationContext(
                        Model,
                        Logger,
                        linqOperatorProvider,
                        resultOperatorHandler,
                        EntityMaterializerSource,
                        ClrPropertyGetterSource,
                        EntityKeyFactorySource,
                        queryMethodProvider,
                        compositeMethodCallTranslator,
                        compositeMemberTranslator,
                        compositeExpressionFragmentTranslator,
                        ValueBufferFactoryFactory,
                        TypeMapper,
                        RelationalExtensions);
        }

        private class FakeQueryCompilationContext : RelationalQueryCompilationContext
        {
            public FakeQueryCompilationContext(
                IModel model,
                ILogger logger,
                ILinqOperatorProvider linqOperatorProvider,
                IResultOperatorHandler resultOperatorHandler,
                IEntityMaterializerSource entityMaterializerSource,
                IClrAccessorSource<IClrPropertyGetter> clrPropertyGetterSource,
                IEntityKeyFactorySource entityKeyFactorySource,
                IQueryMethodProvider queryMethodProvider,
                IMethodCallTranslator compositeMethodCallTranslator,
                IMemberTranslator compositeMemberTranslator,
                IExpressionFragmentTranslator compositeExpressionFragmentTranslator,
                IRelationalValueBufferFactoryFactory valueBufferFactoryFactory,
                IRelationalTypeMapper typeMapper,
                IRelationalMetadataExtensionProvider relationalExtensions)
                : base(
                    model,
                    logger,
                    linqOperatorProvider,
                    resultOperatorHandler,
                    entityMaterializerSource,
                    entityKeyFactorySource,
                    clrPropertyGetterSource,
                    queryMethodProvider,
                    compositeMethodCallTranslator,
                    compositeMemberTranslator,
                    compositeExpressionFragmentTranslator,
                    valueBufferFactoryFactory,
                    typeMapper,
                    relationalExtensions)
            {
            }
        }
    }
}
