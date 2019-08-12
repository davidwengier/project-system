﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks.Dataflow;

using Moq;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static class IPackageRestoreUnconfiguredInputDataSourceFactory
    {
        public static IPackageRestoreUnconfiguredInputDataSource Create()
        {
            var sourceBlock = Mock.Of<IReceivableSourceBlock<IProjectVersionedValue<PackageRestoreUnconfiguredInput>>>();

            // Moq gets really confused with mocking IProjectValueDataSource<IVsProjectRestoreInfo2>.SourceBlock
            // because of the generic/non-generic version of it. Avoid it.
            return new PackageRestoreUnconfiguredDataSource(sourceBlock);
        }

        private class PackageRestoreUnconfiguredDataSource : IPackageRestoreUnconfiguredInputDataSource
        {
            public PackageRestoreUnconfiguredDataSource(IReceivableSourceBlock<IProjectVersionedValue<PackageRestoreUnconfiguredInput>> sourceBlock)
            {
                SourceBlock = sourceBlock;
            }

            public IReceivableSourceBlock<IProjectVersionedValue<PackageRestoreUnconfiguredInput>> SourceBlock { get; }

            public NamedIdentity DataSourceKey { get; }

            public IComparable DataSourceVersion { get; }

            ISourceBlock<IProjectVersionedValue<object>> IProjectValueDataSource.SourceBlock { get; }

            public IDisposable Join()
            {
                return null;
            }
        }

    }
}
