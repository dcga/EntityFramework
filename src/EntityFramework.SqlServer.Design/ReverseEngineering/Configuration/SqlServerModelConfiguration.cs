﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Builders;
using Microsoft.Data.Entity.Relational.Design.ReverseEngineering.Configuration;
using Microsoft.Data.Entity.Relational.Design.Utilities;
using Microsoft.Data.Entity.Relational.Design.Templating;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.SqlServer.Design.ReverseEngineering.Configuration
{
    public class SqlServerModelConfiguration : ModelConfiguration
    {
        private const string _dbContextSuffix = "Context";

        public SqlServerModelConfiguration(
            [NotNull] IModel model,
            [NotNull] CustomConfiguration customConfiguration,
            [NotNull] IRelationalMetadataExtensionProvider extensionsProvider,
            [NotNull] CSharpUtilities cSharpUtilities,
            [NotNull] ModelUtilities modelUtilities)
            : base(model, customConfiguration, extensionsProvider, cSharpUtilities, modelUtilities)
        {
        }

        public override string DefaultSchemaName => "dbo";
        public override string UseMethodName => nameof(SqlServerDbContextOptionsExtensions.UseSqlServer);

        public override string ClassName()
        {
            if (CustomConfiguration.ContextClassName != null)
            {
                return CustomConfiguration.ContextClassName;
            }

            var builder = new SqlConnectionStringBuilder(CustomConfiguration.ConnectionString);
            if (builder.InitialCatalog != null)
            {
                return CSharpUtilities.GenerateCSharpIdentifier(
                    builder.InitialCatalog + _dbContextSuffix, null);
            }

            return base.ClassName();
        }

        public override void AddValueGeneratedConfiguration(
            [NotNull] PropertyConfiguration propertyConfiguration)
        {
            Check.NotNull(propertyConfiguration, nameof(propertyConfiguration));

            // If this property is the single integer primary key on the EntityType then
            // KeyConvention assumes ValueGeneratedOnAdd(). If the underlying column does
            // not have Identity set then we need to set to ValueGeneratedNever() to
            // override this behavior.
            if (propertyConfiguration.Property.SqlServer().IdentityStrategy == null
                && _keyConvention.ValueGeneratedOnAddProperty(
                    new List<Property> { (Property)propertyConfiguration.Property },
                    (EntityType)propertyConfiguration.EntityConfiguration.EntityType) != null)
            {
                propertyConfiguration.FluentApiConfigurations.Add(
                    new FluentApiConfiguration(nameof(PropertyBuilder.ValueGeneratedNever)));
            }
            else
            {
                base.AddValueGeneratedConfiguration(propertyConfiguration);
            }
        }
    }
}
