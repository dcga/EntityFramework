﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational.Design.ReverseEngineering;
using Microsoft.Data.Entity.Sqlite.Metadata;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Data.Entity.Sqlite.Design.ReverseEngineering
{
    public class SqliteDesignTimeMetadataProviderFactory : DesignTimeMetadataProviderFactory
    {
        public override void AddMetadataProviderServices([NotNull] IServiceCollection serviceCollection)
        {
            base.AddMetadataProviderServices(serviceCollection);
            serviceCollection
                .AddScoped<IDatabaseMetadataModelProvider, SqliteMetadataModelProvider>()
                .AddScoped<SqliteReverseTypeMapper>()
                .AddScoped<IRelationalMetadataExtensionProvider, SqliteMetadataExtensionProvider>()
                .AddScoped<ModelConfigurationFactory, SqliteModelConfigurationFactory>();
        }
    }
}
