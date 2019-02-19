﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class SourceItemHandler_CommandTests : CommandLineHandlerTestBase
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new SourceItemHandler((UnconfiguredProject)null);
            });
        }

        [Fact]
        public void UniqueSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onSourceFileAdded(string s) => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            void onSourceFileRemoved(string s) => sourceFilesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);
            var logger = Mock.Of<IProjectLogger>();

            var handler = CreateInstance(project, context);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file2.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var empty = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(10, added: added, removed: empty, isActiveContext: true, logger: logger);

            AssertEx.CollectionLength(sourceFilesPushedToWorkspace, 2);
            Assert.Contains(@"C:\file1.cs", sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);

            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            handler.Handle(10, added: empty, removed: removed, isActiveContext: true, logger: logger);

            Assert.Single(sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);
        }

        [Fact]
        public void RootedSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onSourceFileAdded(string s) => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            void onSourceFileRemoved(string s) => sourceFilesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);
            var logger = Mock.Of<IProjectLogger>();

            var handler = CreateInstance(project, context);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"file1.cs", @"..\ProjectFolder\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(10, added: added, removed: removed, isActiveContext: true, logger: logger);

            Assert.Single(sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\file1.cs", sourceFilesPushedToWorkspace);
        }

        internal override ICommandLineHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private SourceItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project = project ?? UnconfiguredProjectFactory.Create();

            var handler = new SourceItemHandler(project);
            if (context != null)
                handler.Initialise(context);

            return handler;
        }
    }
}
