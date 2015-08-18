// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Query
{
    public class RelationalSyncAsyncServices : SyncAsyncServices, IRelationalSyncAsyncServices
    {
        private readonly QueryMethodProvider _queryMethodProvider;
        private readonly AsyncQueryMethodProvider _asyncQueryMethodProvider;


        public RelationalSyncAsyncServices(
            [NotNull] LinqOperatorProvider linqOperatorProvider,
            [NotNull] AsyncLinqOperatorProvider asyncLinqOperatorProvider,
            [NotNull] QueryMethodProvider queryMethodProvider,
            [NotNull] AsyncQueryMethodProvider asyncQueryMethodProvider)
            : base(
                  Check.NotNull(linqOperatorProvider, nameof(linqOperatorProvider)),
                  Check.NotNull(asyncLinqOperatorProvider, nameof(asyncLinqOperatorProvider)))
        {
            Check.NotNull(queryMethodProvider, nameof(queryMethodProvider));
            Check.NotNull(asyncQueryMethodProvider, nameof(asyncQueryMethodProvider));

            _queryMethodProvider = queryMethodProvider;
            _asyncQueryMethodProvider = asyncQueryMethodProvider;
        }

        public virtual IQueryMethodProvider QueryMethodProvider
            => GetService<IQueryMethodProvider>(_queryMethodProvider, _asyncQueryMethodProvider);
    }
}
