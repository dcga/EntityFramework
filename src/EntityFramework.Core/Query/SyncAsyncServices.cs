// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Query
{
    public class SyncAsyncServices : ISyncAsyncServices
    {
        private readonly LinqOperatorProvider _linqOperatorProvider;
        private readonly AsyncLinqOperatorProvider _asyncLinqOperatorProvider;

        private bool? _isAsync;

        public SyncAsyncServices(
            [NotNull] LinqOperatorProvider linqOperatorProvider,
            [NotNull] AsyncLinqOperatorProvider asyncLinqOperatorProvider)
        {
            Check.NotNull(asyncLinqOperatorProvider, nameof(asyncLinqOperatorProvider));
            Check.NotNull(linqOperatorProvider, nameof(linqOperatorProvider));

            _asyncLinqOperatorProvider = asyncLinqOperatorProvider;
            _linqOperatorProvider = linqOperatorProvider;
        }
        public virtual IDisposable BeginScope(bool isAsync = false)
        {
            _isAsync = isAsync;
            return new SyncAsyncScope(this);
        }

        protected TService GetService<TService>(TService syncService, TService asyncService)
        {
            if (!_isAsync.HasValue)
            {
                throw new InvalidOperationException();
            }

            return _isAsync.Value
                ? asyncService
                : syncService;
        }

        public virtual ILinqOperatorProvider LinqOperatorProvider
            => GetService<ILinqOperatorProvider>(_linqOperatorProvider, _asyncLinqOperatorProvider);

        private class SyncAsyncScope : IDisposable
        {
            private readonly SyncAsyncServices _syncAsyncServices;

            public SyncAsyncScope(SyncAsyncServices syncAsyncServices)
            {
                _syncAsyncServices = syncAsyncServices;
            }

            public void Dispose()
            {
                _syncAsyncServices._isAsync = null;
            }
        }
    }
}
