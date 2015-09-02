﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Data.Entity.Relational.Design.FunctionalTests.ReverseEngineering;
using Xunit.Abstractions;

namespace EntityFramework.Sqlite.Design.FunctionalTests.ReverseEngineering
{
    public class SqliteAttributesInsteadOfFluentApiE2ETest : SqliteE2ETestBase
    {
        public SqliteAttributesInsteadOfFluentApiE2ETest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override string DbSuffix { get; } = "Attributes";
        protected override string TemplateDir { get; } = null; // do not use templates for this test
        protected override string ExpectedResultsParentDir { get; } = Path.Combine("ReverseEngineering", "Expected", "UseAttributesInsteadOfFluentApi");

        protected override string ProviderName => "EntityFramework.Sqlite.Design";
        protected override LoggerMessages ExpectedLoggerMessages { get; } = new LoggerMessages();
    }
}
