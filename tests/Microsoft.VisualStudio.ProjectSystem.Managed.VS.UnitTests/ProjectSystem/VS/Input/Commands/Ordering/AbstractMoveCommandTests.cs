﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    public abstract class AbstractMoveCommandTests
    {
        [Fact]
        public void Constructor_NullAsProjectTree_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(null, SVsServiceProviderFactory.Create(),
                ConfiguredProjectFactory.Create(), IProjectAccessorFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsSVsServiceProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(IPhysicalProjectTreeFactory.Create(), null,
                ConfiguredProjectFactory.Create(), IProjectAccessorFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsConfiguredProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(IPhysicalProjectTreeFactory.Create(),
                SVsServiceProviderFactory.Create(), null, IProjectAccessorFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsAccessor_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(IPhysicalProjectTreeFactory.Create(),
                SVsServiceProviderFactory.Create(), ConfiguredProjectFactory.Create(), null));
        }

        abstract internal long GetCommandId();

        internal AbstractMoveCommand CreateAbstractInstance(IPhysicalProjectTree projectTree = null, Shell.SVsServiceProvider serviceProvider = null, ConfiguredProject configuredProject = null, IProjectAccessor accessor = null)
        {
            projectTree ??= IPhysicalProjectTreeFactory.Create();
            serviceProvider ??= SVsServiceProviderFactory.Create();
            configuredProject ??= ConfiguredProjectFactory.Create();
            accessor ??= IProjectAccessorFactory.Create();

            return CreateInstance(projectTree, serviceProvider, configuredProject, accessor);
        }

        internal abstract AbstractMoveCommand CreateInstance(IPhysicalProjectTree projectTree, Shell.SVsServiceProvider serviceProvider, ConfiguredProject configuredProject, IProjectAccessor accessor);
    }
}
