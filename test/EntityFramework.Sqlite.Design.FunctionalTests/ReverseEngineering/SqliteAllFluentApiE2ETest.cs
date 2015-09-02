﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Data.Entity.Relational.Design.FunctionalTests.ReverseEngineering;
using Microsoft.Data.Entity.Relational.Design.ReverseEngineering;
using Microsoft.Data.Entity.Sqlite.Design.ReverseEngineering;
using Xunit.Abstractions;

namespace EntityFramework.Sqlite.Design.FunctionalTests.ReverseEngineering
{
    public class SqliteAllFluentApiE2ETest : SqliteE2ETestBase
    {
        public SqliteAllFluentApiE2ETest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override string DbSuffix { get; } = "FluentApi";
        protected override string TemplateDir { get; } = "TemplateDir";
        protected override string ExpectedResultsParentDir { get; } = Path.Combine("ReverseEngineering", "Expected", "AllFluentApi");

        protected override string ProviderName => "EntityFramework.Sqlite.Design";
        protected override IDesignTimeMetadataProviderFactory GetFactory() => new SqliteDesignTimeMetadataProviderFactory();
        protected override LoggerMessages ExpectedLoggerMessages
        {
            get
            {
                return new LoggerMessages
                {
                    Info =
                        {
                            "Using custom template " + Path.Combine(TemplateDir, ProviderDbContextTemplateName),
                            "Using custom template " + Path.Combine(TemplateDir, ProviderEntityTypeTemplateName)
                        }
                };
            }
        }
    }
}
