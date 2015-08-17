﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Query.Sql;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Data.Entity.Query
{
    public class CommandBuilderFactory : ICommandBuilderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CommandBuilderFactory([NotNull] IServiceProvider serviceProvider)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
        }

        public virtual CommandBuilder Create([NotNull] Func<ISqlQueryGenerator> sqlGeneratorFactory)
        {
            var builder = _serviceProvider.GetService<CommandBuilder>();

            builder.Initialize(sqlGeneratorFactory);

            return builder;
        }
    }
}
