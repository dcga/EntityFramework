// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Query.Sql;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Query
{
    public class CommandBuilder
    {
        private readonly IRelationalValueBufferFactoryFactory _valueBufferFactoryFactory;

        private Func<ISqlQueryGenerator> _sqlGeneratorFunc;
        private IRelationalValueBufferFactory _valueBufferFactory;

        public CommandBuilder([NotNull] IRelationalValueBufferFactoryFactory valueBufferFactoryFactory)
        {
            Check.NotNull(valueBufferFactoryFactory, nameof(valueBufferFactoryFactory));

            _valueBufferFactoryFactory = valueBufferFactoryFactory;
        }

        public virtual void Initialize([NotNull] Func<ISqlQueryGenerator> sqlGeneratorFunc)
        {
            Check.NotNull(sqlGeneratorFunc, nameof(sqlGeneratorFunc));

            _sqlGeneratorFunc = sqlGeneratorFunc;
        }

        public virtual IRelationalValueBufferFactory ValueBufferFactory => _valueBufferFactory;

        public virtual Func<ISqlQueryGenerator> SqlGeneratorFunc => _sqlGeneratorFunc;

        public virtual DbCommand Build(
            [NotNull] IRelationalConnection connection,
            [NotNull] IDictionary<string, object> parameterValues)
        {
            Check.NotNull(connection, nameof(connection));

            // TODO: Cache command...

            var command = connection.DbConnection.CreateCommand();

            if (connection.Transaction != null)
            {
                command.Transaction = connection.Transaction.DbTransaction;
            }

            if (connection.CommandTimeout != null)
            {
                command.CommandTimeout = (int)connection.CommandTimeout;
            }

            var sqlQueryGenerator = _sqlGeneratorFunc();

            command.CommandText = sqlQueryGenerator.GenerateSql(parameterValues);

            foreach (var commandParameter in sqlQueryGenerator.Parameters)
            {
                command.Parameters.Add(
                    commandParameter.TypeMapping.CreateParameter(
                        command,
                        commandParameter.Name,
                        commandParameter.Value));
            }

            return command;
        }

        public virtual void NotifyReaderCreated([NotNull] DbDataReader dataReader)
        {
            Check.NotNull(dataReader, nameof(dataReader));

            LazyInitializer
                .EnsureInitialized(
                    ref _valueBufferFactory,
                    () => _sqlGeneratorFunc()
                        .CreateValueBufferFactory(_valueBufferFactoryFactory, dataReader));
        }
    }
}
