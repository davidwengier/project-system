﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using Microsoft.VisualStudio.Shell.Interop;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class UnconfiguredProjectVsServicesTests
    {
        [Fact]
        public void Constructor_ValueAsUnconfiguredProject_SetsVsHierarchyToHostObject()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            var project = UnconfiguredProjectFactory.Create(hostObject: hierarchy);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(hierarchy, vsServices.VsHierarchy);
        }

        [Fact]
        public void Constructor_ValueAsUnconfiguredProject_SetsVsProjectToHostObject()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            var project = UnconfiguredProjectFactory.Create(hostObject: hierarchy);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(hierarchy, vsServices.VsProject);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsProjectToCommonServicesProject()
        {
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(project, vsServices.Project);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsThreadingServiceToCommonServicesThreadingService()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(threadingService: threadingService);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(threadingService, vsServices.ThreadingService);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsActiveConfiguredProjectProjectToCommonServicesActiveConfiguredProject()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(configuredProject: projectProperties.ConfiguredProject);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(projectProperties.ConfiguredProject, vsServices.ActiveConfiguredProject);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsActiveConfiguredProjectPropertiesToCommonServicesActiveConfiguredProjectProperties()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(projectProperties, vsServices.ActiveConfiguredProjectProperties);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsProjectAccessorToCommonServicesProjectAccessor()
        {
            var projectAccessor = IProjectAccessorFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectAccessor: projectAccessor);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(projectAccessor, vsServices.ProjectAccessor);
        }


        private static UnconfiguredProjectVsServices CreateInstance(IUnconfiguredProjectCommonServices commonServices)
        {
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            return new UnconfiguredProjectVsServices(commonServices, projectTree);
        }
    }
}
