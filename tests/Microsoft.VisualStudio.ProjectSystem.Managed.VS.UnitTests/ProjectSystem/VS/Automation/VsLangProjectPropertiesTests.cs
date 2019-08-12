﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Moq;

using VSLangProj;

using VSLangProj110;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    public class VSProject_VSLangProjectPropertiesTests
    {
        [Fact]
        public void NotNull()
        {
            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.Setup(p => p.Capabilities)
                                   .Returns((IProjectCapabilitiesScope)null);

            var vsproject = CreateInstance(
                                Mock.Of<VSLangProj.VSProject>(),
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.NotNull(vsproject);
        }

        [Fact]
        public void ImportsAndEventsAsNull()
        {
            var imports = Mock.Of<Imports>();
            var events = Mock.Of<VSProjectEvents>();
            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();

            innerVSProjectMock.Setup(p => p.Imports)
                              .Returns(imports);

            innerVSProjectMock.Setup(p => p.Events)
                              .Returns(events);

            var vsproject = CreateInstance(
                                innerVSProjectMock.Object,
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.NotNull(vsproject);
            Assert.True(imports.Equals(vsproject.Imports));
            Assert.Equal(events, vsproject.Events);
        }


        [Fact]
        public void ImportsAndEventsAsNonNull()
        {
            var imports = Mock.Of<Imports>();
            var importsImpl = new OrderPrecedenceImportCollection<Imports>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject)null)
            {
                new Lazy<Imports, IOrderPrecedenceMetadataView>(() => imports, IOrderPrecedenceMetadataViewFactory.Create("VisualBasic"))
            };
            var events = Mock.Of<VSProjectEvents>();
            var vsProjectEventsImpl = new OrderPrecedenceImportCollection<VSProjectEvents>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject)null)
            {
                new Lazy<VSProjectEvents, IOrderPrecedenceMetadataView>(() => events, IOrderPrecedenceMetadataViewFactory.Create("VisualBasic"))
            };

            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();

            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.Setup(p => p.Capabilities)
                                   .Returns((IProjectCapabilitiesScope)null);

            var vsproject = new VSProjectTestImpl(
                                innerVSProjectMock.Object,
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>(),
                                project: unconfiguredProjectMock.Object,
                                buildManager: Mock.Of<BuildManager>());

            vsproject.SetImportsImpl(importsImpl);
            vsproject.SetVSProjectEventsImpl(vsProjectEventsImpl);

            Assert.NotNull(vsproject);
            Assert.True(imports.Equals(vsproject.Imports));
            Assert.Equal(events, vsproject.Events);
        }

        [Fact]
        public void OutputTypeEx()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneralBrowseObject.SchemaName, ConfigurationGeneralBrowseObject.OutputTypeProperty, 4, setValues);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(prjOutputTypeEx.prjOutputTypeEx_AppContainerExe, vsLangProjectProperties.OutputTypeEx);

            vsLangProjectProperties.OutputTypeEx = prjOutputTypeEx.prjOutputTypeEx_WinExe;
            Assert.Equal(setValues.Single().ToString(), prjOutputTypeEx.prjOutputTypeEx_WinExe.ToString());
        }

        [Fact]
        public void OutputType()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneralBrowseObject.SchemaName, ConfigurationGeneralBrowseObject.OutputTypeProperty, 1, setValues);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(prjOutputType.prjOutputTypeExe, vsLangProjectProperties.OutputType);

            vsLangProjectProperties.OutputType = prjOutputType.prjOutputTypeLibrary;
            Assert.Equal(prjOutputType.prjOutputTypeLibrary, setValues.Single());
        }

        [Fact]
        public void AssemblyName()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.AssemblyNameProperty, "Blah", setValues);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal("Blah", vsLangProjectProperties.AssemblyName);

            var testValue = "Testing";
            vsLangProjectProperties.AssemblyName = testValue;
            Assert.Equal(setValues.Single(), testValue);
        }

        [Fact]
        public void FullPath()
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.ProjectDirProperty, "somepath");

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal("somepath", vsLangProjectProperties.FullPath);
        }

        [Fact]
        public void AbsoluteProjectDirectory()
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneralBrowseObject.SchemaName, ConfigurationGeneralBrowseObject.FullPathProperty, "testvalue");

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal("testvalue", vsLangProjectProperties.AbsoluteProjectDirectory);
        }

        [Fact]
        public void ExtenderCATID()
        {
            var vsproject = CreateInstance(
                Mock.Of<VSLangProj.VSProject>(),
                threadingService: Mock.Of<IProjectThreadingService>(),
                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>(),
                buildManager: Mock.Of<BuildManager>());
            Assert.Null(vsproject.ExtenderCATID);
        }

        private static VSProject CreateInstance(
            VSLangProj.VSProject vsproject = null,
            IProjectThreadingService threadingService = null,
            ActiveConfiguredProject<ProjectProperties> projectProperties = null,
            UnconfiguredProject project = null,
            BuildManager buildManager = null)
        {
            if (project == null)
            {
                project = UnconfiguredProjectFactory.Create();
            }

            return new VSProject(vsproject, threadingService, projectProperties, project, buildManager);
        }

        internal class VSProjectTestImpl : VSProject
        {
            public VSProjectTestImpl(
                VSLangProj.VSProject vsProject,
                IProjectThreadingService threadingService,
                ActiveConfiguredProject<ProjectProperties> projectProperties,
                UnconfiguredProject project,
                BuildManager buildManager)
                : base(vsProject, threadingService, projectProperties, project, buildManager)
            {
            }

            internal void SetImportsImpl(OrderPrecedenceImportCollection<Imports> importsImpl)
            {
                ImportsImpl = importsImpl;
            }

            internal void SetVSProjectEventsImpl(OrderPrecedenceImportCollection<VSProjectEvents> vsProjectEventsImpl)
            {
                VSProjectEventsImpl = vsProjectEventsImpl;
            }
        }
    }
}
