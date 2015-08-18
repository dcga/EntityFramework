// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Query.Expressions;
using Microsoft.Data.Entity.Query.Internal;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;
using Remotion.Linq.Clauses;

namespace Microsoft.Data.Entity.Query
{
    public class RelationalQueryCompilationContext : QueryCompilationContext
    {
        private readonly List<RelationalQueryModelVisitor> _relationalQueryModelVisitors
            = new List<RelationalQueryModelVisitor>();

        private IQueryMethodProvider _queryMethodProvider;

        public RelationalQueryCompilationContext(
            [NotNull] QueryCompilationContextServices services,
            [NotNull] ILoggerFactory loggerFactory)
            : base(services, loggerFactory)
        {
        }

        public override void Initialize(bool isAsync)
        {
            base.Initialize(isAsync);

            if (isAsync)
            {
                _queryMethodProvider = new AsyncQueryMethodProvider();
            }
            else
            {
                _queryMethodProvider = new QueryMethodProvider();
            }
        }

        public override EntityQueryModelVisitor CreateQueryModelVisitor()
        {
            var relationalQueryModelVisitor =
                (RelationalQueryModelVisitor)base.CreateQueryModelVisitor();

            _relationalQueryModelVisitors.Add(relationalQueryModelVisitor);

            return relationalQueryModelVisitor;
        }

        public override EntityQueryModelVisitor CreateQueryModelVisitor(EntityQueryModelVisitor parentEntityQueryModelVisitor)
        {
            var relationalQueryModelVisitor =
                (RelationalQueryModelVisitor)base.CreateQueryModelVisitor(parentEntityQueryModelVisitor);

            _relationalQueryModelVisitors.Add(relationalQueryModelVisitor);

            return relationalQueryModelVisitor;
        }

        public override IExpressionPrinter CreateExpressionPrinter()
        {
            return new RelationalExpressionPrinter();
        }

        public virtual SelectExpression FindSelectExpression([NotNull] IQuerySource querySource)
        {
            Check.NotNull(querySource, nameof(querySource));

            return
                (from v in _relationalQueryModelVisitors
                 let selectExpression = v.TryGetQuery(querySource)
                 where selectExpression != null
                 select selectExpression)
                    .First();
        }

        public virtual IQueryMethodProvider QueryMethodProvider
        {
            get { return _queryMethodProvider; }
            [param: NotNull]
            protected set
            {
                Check.NotNull(value, nameof(value));

                _queryMethodProvider = value;
            }
        }
    }
}
