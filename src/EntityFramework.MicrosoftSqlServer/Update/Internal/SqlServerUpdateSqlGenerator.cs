// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Update.Internal
{
    public class SqlServerUpdateSqlGenerator : UpdateSqlGenerator, ISqlServerUpdateSqlGenerator
    {
        private readonly IRelationalTypeMapper _typeMapper;

        public SqlServerUpdateSqlGenerator([NotNull] ISqlGenerationHelper sqlGenerationHelper,
            [NotNull] IRelationalTypeMapper typeMapper)
            : base(sqlGenerationHelper)
        {
            _typeMapper = typeMapper;
        }

        public virtual ResultSetMapping AppendBulkInsertOperation(
            StringBuilder commandStringBuilder,
            IReadOnlyList<ModificationCommand> modificationCommands,
            int commandPosition)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
            Check.NotEmpty(modificationCommands, nameof(modificationCommands));

            if (modificationCommands.Count == 1
                && modificationCommands[0].ColumnModifications.All(o =>
                    !o.IsKey
                    || !o.IsRead
                    || o.Property.SqlServer().ValueGenerationStrategy == SqlServerValueGenerationStrategy.IdentityColumn))
            {
                return AppendInsertOperation(commandStringBuilder, modificationCommands[0], commandPosition);
            }

            var name = modificationCommands[0].TableName;
            var schema = modificationCommands[0].Schema;

            // TODO: Batch base and derived for TPH
            // #3954
            var defaultValuesOnly = !modificationCommands.First().ColumnModifications.Any(o => o.IsWrite);
            var statementCount = defaultValuesOnly
                ? modificationCommands.Count
                : 1;
            var valueSetCount = defaultValuesOnly
                ? 1
                : modificationCommands.Count;
            var resultSetCreated = false;

            for (var i = 0; i < statementCount; i++)
            {
                var operations = modificationCommands[i].ColumnModifications;
                var writeOperations = operations.Where(o => o.IsWrite).ToArray();
                var readOperations = operations.Where(o => o.IsRead).ToArray();

                if (readOperations.Length > 0)
                {
                    AppendDeclareGeneratedTable(commandStringBuilder, readOperations, commandPosition);
                }

                AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);
                if (readOperations.Length > 0)
                {
                    AppendOutputClause(commandStringBuilder, readOperations, commandPosition);
                }
                AppendValuesHeader(commandStringBuilder, writeOperations);
                AppendValues(commandStringBuilder, writeOperations);
                for (var j = 1; j < valueSetCount; j++)
                {
                    commandStringBuilder.Append(",").AppendLine();
                    AppendValues(commandStringBuilder, modificationCommands[j].ColumnModifications.Where(o => o.IsWrite).ToArray());
                }
                commandStringBuilder.Append(SqlGenerationHelper.StatementTerminator).AppendLine();

                if (readOperations.Length > 0)
                {
                    AppendSelectGeneratedCommand(commandStringBuilder, readOperations, commandPosition);
                    resultSetCreated = true;
                }
            }

            return resultSetCreated ?
                defaultValuesOnly
                    ? ResultSetMapping.LastInResultSet
                    : ResultSetMapping.NotLastInResultSet
                : ResultSetMapping.NoResultSet;
        }

        public override ResultSetMapping AppendUpdateOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
            Check.NotNull(command, nameof(command));

            var name = command.TableName;
            var schema = command.Schema;
            var operations = command.ColumnModifications;

            var writeOperations = operations.Where(o => o.IsWrite).ToArray();
            var conditionOperations = operations.Where(o => o.IsCondition).ToArray();
            var readOperations = operations.Where(o => o.IsRead).ToArray();

            if (readOperations.Length > 0)
            {
                AppendDeclareGeneratedTable(commandStringBuilder, readOperations, commandPosition);
            }
            AppendUpdateCommandHeader(commandStringBuilder, name, schema, writeOperations);
            if (readOperations.Length > 0)
            {
                AppendOutputClause(commandStringBuilder, readOperations, commandPosition);
            }
            AppendWhereClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.Append(SqlGenerationHelper.StatementTerminator).AppendLine();

            if (readOperations.Length > 0)
            {
                return AppendSelectGeneratedCommand(commandStringBuilder, readOperations, commandPosition);
            }
            return AppendSelectAffectedCountCommand(commandStringBuilder, name, schema, commandPosition);
        }

        private void AppendDeclareGeneratedTable(StringBuilder commandStringBuilder, ColumnModification[] readOperations, int commandPosition)
        {
            commandStringBuilder
                .Append($"DECLARE @generated{commandPosition} TABLE (")
                .AppendJoin(readOperations.Select(c =>
                    SqlGenerationHelper.DelimitIdentifier(c.ColumnName) + " " + GetTypeNameForCopy(c.Property)))
                .Append(")")
                .Append(SqlGenerationHelper.StatementTerminator)
                .AppendLine();
        }

        private string GetTypeNameForCopy(IProperty property)
        {
            var mapping = _typeMapper.GetMapping(property);
            var typeName = mapping.DefaultTypeName;
            if (property.IsConcurrencyToken
                && (typeName.Equals("rowversion", StringComparison.OrdinalIgnoreCase)
                    || typeName.Equals("timestamp", StringComparison.OrdinalIgnoreCase)))
            {
                return property.IsNullable ? "varbinary(8)" : "binary(8)";
            }

            return typeName;
        }

        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private void AppendOutputClause(
            StringBuilder commandStringBuilder,
            IReadOnlyList<ColumnModification> operations,
            int commandPosition)
        {
            commandStringBuilder
                .AppendLine()
                .Append("OUTPUT ")
                .AppendJoin(operations.Select(c => "INSERTED." + SqlGenerationHelper.DelimitIdentifier(c.ColumnName)));

            commandStringBuilder
                .AppendLine()
                .Append($"INTO @generated{commandPosition}");
        }

        private ResultSetMapping AppendSelectGeneratedCommand(StringBuilder commandStringBuilder, ColumnModification[] readOperations, int commandPosition)
        {
            commandStringBuilder
                .Append("SELECT ")
                .AppendJoin(readOperations.Select(c => SqlGenerationHelper.DelimitIdentifier(c.ColumnName)))
                .Append($" FROM @generated{commandPosition}")
                .Append(SqlGenerationHelper.StatementTerminator)
                .AppendLine();

            return ResultSetMapping.LastInResultSet;
        }

        protected override ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder, string name, string schema, int commandPosition)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder))
                .Append("SELECT @@ROWCOUNT")
                .Append(SqlGenerationHelper.StatementTerminator).AppendLine();

            return ResultSetMapping.LastInResultSet;
        }

        public override void AppendBatchHeader(StringBuilder commandStringBuilder)
            => Check.NotNull(commandStringBuilder, nameof(commandStringBuilder))
                .Append("SET NOCOUNT ON")
                .Append(SqlGenerationHelper.StatementTerminator).AppendLine();

        protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification)
            => Check.NotNull(commandStringBuilder, nameof(commandStringBuilder))
                .Append(SqlGenerationHelper.DelimitIdentifier(Check.NotNull(columnModification, nameof(columnModification)).ColumnName))
                .Append(" = ")
                .Append("scope_identity()");

        protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
            => Check.NotNull(commandStringBuilder, nameof(commandStringBuilder))
                .Append("@@ROWCOUNT = " + expectedRowsAffected);
    }
}
