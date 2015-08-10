// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Query.Annotations;
using Microsoft.Data.Entity.Query.Internal;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Microsoft.Data.Entity.Query
{
    public abstract class QueryCompilationContext
    {
        private ILinqOperatorProvider _linqOperatorProvider;

        private IReadOnlyCollection<QueryAnnotationBase> _queryAnnotations;
        private IDictionary<IQuerySource, List<IReadOnlyList<INavigation>>> _trackableIncludes;
        private ISet<IQuerySource> _querySourcesRequiringMaterialization;

        protected QueryCompilationContext(
            [NotNull] QueryCompilationContextServices services,
            [NotNull] ILoggerFactory loggerFactory)
        {
            Check.NotNull(services, nameof(services));
            Check.NotNull(loggerFactory, nameof(loggerFactory));

            Services = services;
            Logger = loggerFactory.CreateLogger<Database>();
        }

        public virtual void Initialize(bool isAsync)
        {
            if (isAsync)
            {
                _linqOperatorProvider = new AsyncLinqOperatorProvider();
            }
            else
            {
                _linqOperatorProvider = new LinqOperatorProvider();
            }
        }

        public virtual QueryCompilationContextServices Services { get; }

        public virtual ILogger Logger { get; }
        public virtual ILinqOperatorProvider LinqOperatorProvider
        {
            get { return _linqOperatorProvider; }
            [param: NotNull]
            set
            {
                Check.NotNull(value, nameof(value));

                _linqOperatorProvider = value;
            }
        }

        public virtual QuerySourceMapping QuerySourceMapping { get; } = new QuerySourceMapping();

        public virtual IReadOnlyCollection<QueryAnnotationBase> QueryAnnotations
        {
            get { return _queryAnnotations; }
            [param: NotNull]
            set
            {
                Check.NotNull(value, nameof(value));

                _queryAnnotations = value;
            }
        }

        public virtual IEnumerable<QueryAnnotation> GetCustomQueryAnnotations([NotNull] MethodInfo methodInfo)
            => _queryAnnotations
                .OfType<QueryAnnotation>()
                .Where(qa => qa.IsCallTo(Check.NotNull(methodInfo, nameof(methodInfo))));

        public virtual EntityQueryModelVisitor CreateQueryModelVisitor()
            => Services.EntityQueryModelVisitorFactory.Create(this);

        public virtual EntityQueryModelVisitor CreateQueryModelVisitor([CanBeNull] EntityQueryModelVisitor parentEntityQueryModelVisitor)
            => Services.EntityQueryModelVisitorFactory.Create(parentEntityQueryModelVisitor.QueryCompilationContext, parentEntityQueryModelVisitor);

        public virtual IExpressionPrinter CreateExpressionPrinter()
        {
            return new ExpressionPrinter();
        }

        public virtual void AddTrackableInclude(
            [NotNull] IQuerySource querySource, [NotNull] IReadOnlyList<INavigation> navigationPath)
        {
            Check.NotNull(querySource, nameof(querySource));
            Check.NotNull(navigationPath, nameof(navigationPath));

            if (_trackableIncludes == null)
            {
                _trackableIncludes = new Dictionary<IQuerySource, List<IReadOnlyList<INavigation>>>();
            }

            List<IReadOnlyList<INavigation>> includes;
            if (!_trackableIncludes.TryGetValue(querySource, out includes))
            {
                _trackableIncludes.Add(querySource, includes = new List<IReadOnlyList<INavigation>>());
            }

            includes.Add(navigationPath);
        }

        public virtual IReadOnlyList<IReadOnlyList<INavigation>> GetTrackableIncludes([NotNull] IQuerySource querySource)
        {
            Check.NotNull(querySource, nameof(querySource));

            if (_trackableIncludes == null)
            {
                return null;
            }

            List<IReadOnlyList<INavigation>> includes;

            return _trackableIncludes.TryGetValue(querySource, out includes) ? includes : null;
        }

        public virtual void FindQuerySourcesRequiringMaterialization(
            [NotNull] EntityQueryModelVisitor queryModelVisitor, [NotNull] QueryModel queryModel)
        {
            Check.NotNull(queryModelVisitor, nameof(queryModelVisitor));
            Check.NotNull(queryModel, nameof(queryModel));

            var requiresEntityMaterializationExpressionVisitor
                = Services.RequiresMaterializationExpressionVisitorFactory.Create(queryModelVisitor);

            _querySourcesRequiringMaterialization
                = requiresEntityMaterializationExpressionVisitor
                    .FindQuerySourcesRequiringMaterialization(queryModel);

            foreach (var groupJoinClause in queryModel.BodyClauses.OfType<GroupJoinClause>())
            {
                _querySourcesRequiringMaterialization.Add(groupJoinClause.JoinClause);
            }
        }

        public virtual bool QuerySourceRequiresMaterialization([NotNull] IQuerySource querySource)
        {
            Check.NotNull(querySource, nameof(querySource));

            return _querySourcesRequiringMaterialization.Contains(querySource);
        }
    }
}
