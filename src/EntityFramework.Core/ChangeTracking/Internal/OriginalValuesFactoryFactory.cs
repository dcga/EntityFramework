// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;

namespace Microsoft.Data.Entity.ChangeTracking.Internal
{
    public class OriginalValuesFactoryFactory : SnapshotFactoryFactory<InternalEntityEntry>
    {
        protected override int GetPropertyIndex(IPropertyBase propertyBase)
            => (propertyBase as IProperty)?.GetOriginalValueIndex() ?? -1;

        protected override int GetPropertyCount(IEntityType entityType) 
            => entityType.OriginalValueCount();
    }
}
