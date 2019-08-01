﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Telemetry;

using Moq;

using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    public class TempPECompilerManagerTests : IDisposable
    {
        private const int TestTimeoutMillisecondsDelay = 1000;

        private string? _lastIntermediateOutputPath;
        private readonly string _projectFolder = @"C:\MyProject";
        private string _intermediateOutputPath = "MyOutput";
        private readonly IFileSystemMock _fileSystem;
        private readonly DesignTimeInputsChangeTracker _manager;

        // For tracking compilation events that occur, to verify
        private readonly List<(string OutputFileName, string[] SourceFiles)> _compilationResults = new List<(string, string[])>();
        private TaskCompletionSource<bool>? _compilationOccurredCompletionSource;
        private int _expectedCompilations;
        private Func<string, ISet<string>, bool> _compilationCallback;

        [Fact]
        public async Task SingleDesignTimeInput_ShouldCompile()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyProjectChangeCausesCompilation(1, inputs);

            Assert.Single(_compilationResults);
            Assert.Single(_compilationResults[0].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), _compilationResults[0].OutputFileName);
        }

        [Fact]
        public async Task SingleDesignTimeInput_OutputDoesntExist_ShouldCompile()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyProjectChangeCausesCompilation(1, inputs);

            string tempPEDescriptionXml = await _manager.GetDesignTimeInputXmlAsync("File1.cs");

            // This also validates that getting the description didn't force a compile, because the output is up to date
            Assert.Single(_compilationResults);
            Assert.Single(_compilationResults[0].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), _compilationResults[0].OutputFileName);

            Assert.Equal(@"<root>
  <Application private_binpath = ""C:\MyProject\MyOutput\TempPE""/>
  <Assembly
    codebase = ""File1.cs.dll""
    name = ""File1.cs""
    version = ""0.0.0.0""
    snapshot_id = ""1""
    replaceable = ""True""
  />
</root>", tempPEDescriptionXml);
        }

        [Fact]
        public async Task SingleDesignTimeInput_OutputDoesntExist_ShouldCompileWhenGettingXML()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyProjectChangeCausesCompilation(1, inputs);

            // If the first compile didn't happen, our test results won't be valid
            Assert.Single(_compilationResults);
            Assert.Single(_compilationResults[0].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), _compilationResults[0].OutputFileName);

            // Remove the output file, should mean that getting the XML forces a compile
            _fileSystem.RemoveFile(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"));

            string tempPEDescriptionXml = await _manager.GetDesignTimeInputXmlAsync("File1.cs");

            // Verify a second compile happened
            Assert.Equal(2, _compilationResults.Count);
            Assert.Single(_compilationResults[1].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[1].SourceFiles);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), _compilationResults[1].OutputFileName);

            Assert.Equal(@"<root>
  <Application private_binpath = ""C:\MyProject\MyOutput\TempPE""/>
  <Assembly
    codebase = ""File1.cs.dll""
    name = ""File1.cs""
    version = ""0.0.0.0""
    snapshot_id = ""1""
    replaceable = ""True""
  />
</root>", tempPEDescriptionXml);
        }

        [Fact]
        public async Task SingleDesignTimeInput_Changes_ShouldCompileTwice()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyProjectChangeCausesCompilation(1, inputs);

            await VerifyFileChangeCausesCompilation(1, "File1.cs");

            Assert.Equal(2, _compilationResults.Count);
            Assert.Single(_compilationResults[0].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), _compilationResults[0].OutputFileName);

            Assert.Single(_compilationResults[1].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[1].SourceFiles);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), _compilationResults[1].OutputFileName);
        }

        [Fact]
        public async Task NoDesignTimeInputs_NeverCompiles()
        {
            var inputs = new DesignTimeInputs(new string[] { }, new string[] { });

            await VerifyProjectChangeCausesCompilation(0, inputs);

            Assert.Empty(_compilationResults);
        }

        [Fact]
        public async Task SingleDesignTimeInput_OutputUpToDate_ShouldntCompile()
        {
            _fileSystem.AddFile(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), DateTime.UtcNow.AddMinutes(10));

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyProjectChangeCausesCompilation(0, inputs);

            Assert.Empty(_compilationResults);
        }

        [Fact]
        public async Task SingleDesignTimeInput_OutputOutOfDate_ShouldCompile()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            _fileSystem.AddFile(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), DateTime.UtcNow.AddMinutes(-10));

            await VerifyProjectChangeCausesCompilation(1, inputs);

            Assert.Single(_compilationResults);
            Assert.Single(_compilationResults[0].SourceFiles);
        }

        [Fact]
        public async Task TwoDesignTimeInputs_SharedInputChanges_BothCompiled()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs", "File2.cs" }, new string[] { "SharedFile.cs" });

            await VerifyProjectChangeCausesCompilation(2, inputs);

            await VerifyFileChangeCausesCompilation(2, "SharedFile.cs");

            Assert.Equal(4, _compilationResults.Count);
            Assert.Contains("File2.cs", _compilationResults[0].SourceFiles);
            Assert.DoesNotContain("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Contains("SharedFile.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File2.cs.dll"), _compilationResults[0].OutputFileName);
        }

        [Fact]
        public async Task SingleDesignTimeInput_Removed_ShouldntCompile()
        {
            _manager.CompileSynchronously = false;

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyDLLsCompiled(0, () =>
            {
                SendDesignTimeInputs(inputs);

                SendDesignTimeInputs(new DesignTimeInputs(new string[] { }, new string[] { }));
            });

            Assert.Empty(_compilationResults);
        }

        [Fact]
        public async Task SingleDesignTimeInput_AnotherAdded_ShouldCompileBoth()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyDLLsCompiled(2, () =>
            {
                SendDesignTimeInputs(new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { }));

                SendDesignTimeInputs(new DesignTimeInputs(new string[] { "File1.cs", "File2.cs" }, new string[] { }));
            });

            Assert.Equal(2, _compilationResults.Count);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.DoesNotContain("File2.cs", _compilationResults[0].SourceFiles);
            Assert.Contains("File2.cs", _compilationResults[1].SourceFiles);
            Assert.DoesNotContain("File1.cs", _compilationResults[1].SourceFiles);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll"), _compilationResults[0].OutputFileName);
            Assert.Equal(Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File2.cs.dll"), _compilationResults[1].OutputFileName);
        }

        [Fact]
        public async Task TwoDesignTimeInputs_OutputPathChanged_BothCompiled()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs", "File2.cs" }, new string[] { });

            await VerifyProjectChangeCausesCompilation(2, inputs);

            await VerifyProjectChangeCausesCompilation(2, inputs, "NewOutputPath");

            Assert.Equal(4, _compilationResults.Count);
            Assert.Contains("File2.cs", _compilationResults[0].SourceFiles);
            Assert.DoesNotContain("File1.cs", _compilationResults[0].SourceFiles);
        }

        [Fact]
        public async Task NewSharedDesignTimeInput_ModifiedInThePast_ShouldCompileAll()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyProjectChangeCausesCompilation(1, inputs);

            // Add an existing, old, file as a shared input which should cause a compilation even though the DLL is newer than its inputs
            _fileSystem.AddFile(Path.Combine(_projectFolder, "OldFile.cs"), DateTime.UtcNow.AddMinutes(-10));

            inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { "OldFile.cs" });

            await VerifyProjectChangeCausesCompilation(1, inputs);


            Assert.Equal(2, _compilationResults.Count);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.DoesNotContain("OldFile.cs", _compilationResults[0].SourceFiles);

            Assert.Contains("File1.cs", _compilationResults[1].SourceFiles);
            Assert.Contains("OldFile.cs", _compilationResults[1].SourceFiles);
        }

        [Fact]
        public async Task SingleDesignTimeInput_CompileFailed_ShouldDeleteOutputFile()
        {
            var outputPath = Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll");
            _fileSystem.AddFile(outputPath, DateTime.UtcNow.AddMinutes(-10));

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            // We want our compilation to fail
            _compilationCallback = (x, z) => false;

            await VerifyProjectChangeCausesCompilation(0, inputs);

            Assert.Empty(_compilationResults);
            Assert.False(_fileSystem.FileExists(outputPath));
        }

        [Fact]
        public async Task SingleDesignTimeInput_CompileCancelled_ShouldDeleteOutputFile()
        {
            var outputPath = Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll");
            _fileSystem.AddFile(outputPath, DateTime.UtcNow.AddMinutes(-10));

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            // We want our compilation to throw
            _compilationCallback = (x, z) => throw new OperationCanceledException("Boom!");

            await VerifyProjectChangeCausesCompilation(0, inputs);

            Assert.Empty(_compilationResults);
            Assert.False(_fileSystem.FileExists(outputPath));
        }

        [Fact]
        public async Task SingleDesignTimeInput_CompileThrows_ShouldDeleteOutputFile()
        {
            var outputPath = Path.Combine(DesignTimeInputsChangeTracker.GetOutputPath(_projectFolder, _intermediateOutputPath), "File1.cs.dll");
            _fileSystem.AddFile(outputPath, DateTime.UtcNow.AddMinutes(-10));

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            // We want our compilation to throw
            _compilationCallback = (x, z) => throw new IOException("Boom!");

            await VerifyProjectChangeCausesCompilation(0, inputs);

            Assert.Empty(_compilationResults);
            Assert.False(_fileSystem.FileExists(outputPath));
        }

        public TempPECompilerManagerTests()
        {
            _compilationCallback = CompilationCallBack;

            _fileSystem = new IFileSystemMock();

            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            using var designTimeInputsSource = ProjectValueDataSourceFactory.Create<DesignTimeInputs>(services);

            var dataSourceMock = new Mock<IDesignTimeInputsDataSource>();
            dataSourceMock.SetupGet(s => s.SourceBlock)
                .Returns(designTimeInputsSource.SourceBlock);

            using var fileWatcherSource = ProjectValueDataSourceFactory.Create<string[]>(services);

            var watcherMock = new Mock<IDesignTimeInputsFileWatcher>();
            watcherMock.SetupGet(s => s.SourceBlock)
                .Returns(fileWatcherSource.SourceBlock);

            var telemetryService = ITelemetryServiceFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var projectSubscriptionService = IActiveConfiguredProjectSubscriptionServiceFactory.Create();
            var activeWorkspaceProjectContextHost = IActiveWorkspaceProjectContextHostFactory.ImplementProjectContextAccessor(IWorkspaceProjectContextAccessorFactory.Create());
            var unconfiguredProject = UnconfiguredProjectFactory.Create(
                filePath: Path.Combine(_projectFolder, "MyTestProj.csproj"),
                projectAsynchronousTasksService: IProjectAsynchronousTasksServiceFactory.Create());

            var compilerMock = new Mock<ITempPECompiler>();
            compilerMock.Setup(c => c.CompileAsync(It.IsAny<IWorkspaceProjectContext>(), It.IsAny<string>(), It.IsAny<ISet<string>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((IWorkspaceProjectContext context, string outputFile, ISet<string> filesToCompile, CancellationToken token) => _compilationCallback(outputFile, filesToCompile));

            _manager = new DesignTimeInputsChangeTracker(unconfiguredProject,
                                      projectSubscriptionService,
                                      activeWorkspaceProjectContextHost,
                                      threadingService,
                                      dataSourceMock.Object,
                                      watcherMock.Object,
                                      compilerMock.Object,
                                      _fileSystem,
                                      telemetryService)
            {
                CompileSynchronously = true
            };
        }

        private bool CompilationCallBack(string output, ISet<string> files)
        {
            // "Create" our output file
            _fileSystem.AddFile(output);

            _compilationResults.Add((output, files.Select(f => Path.GetFileName(f)).ToArray()));
            if (_compilationResults.Count == _expectedCompilations)
            {
                _compilationOccurredCompletionSource?.SetResult(true);
            }

            return true;
        }

        private async Task VerifyProjectChangeCausesCompilation(int numberOfDLLsExpected, DesignTimeInputs designTimeInputs, string? intermediateOutputPath = null)
        {
            await VerifyDLLsCompiled(numberOfDLLsExpected, () => SendDesignTimeInputs(designTimeInputs, intermediateOutputPath));
        }

        private async Task VerifyFileChangeCausesCompilation(int numberOfDLLsExpected, params string[] filesToChange)
        {
            await VerifyDLLsCompiled(numberOfDLLsExpected, async () => await SendFileChange(filesToChange));
        }

        private Task VerifyDLLsCompiled(int numberOfDLLs, Action actionThatCausesCompilation)
        {
            return VerifyDLLsCompiled(numberOfDLLs, () =>
            {
                actionThatCausesCompilation();
                return Task.CompletedTask;
            });
        }

        private async Task VerifyDLLsCompiled(int expectedDLLs, Func<Task> actionThatCausesCompilation)
        {
            int initialComplations = _compilationResults.Count;
            _expectedCompilations = initialComplations + expectedDLLs;
            _compilationOccurredCompletionSource = new TaskCompletionSource<bool>();

            await actionThatCausesCompilation();

            // Sadly, we need a timeout
            var delay = Task.Delay(TestTimeoutMillisecondsDelay);

            if (await Task.WhenAny(_compilationOccurredCompletionSource.Task, delay) == delay)
            {
                var actualDLLs = _compilationResults.Count - initialComplations;
                if (expectedDLLs != actualDLLs)
                {
                    throw new AssertActualExpectedException(expectedDLLs, actualDLLs, $"Timed out after {TestTimeoutMillisecondsDelay}ms");
                }
            }
        }

        private void SendDesignTimeInputs(DesignTimeInputs inputs, string? intermediateOutputPath = null)
        {
            _intermediateOutputPath = intermediateOutputPath ?? _intermediateOutputPath;

            var ruleUpdate = @"{
                                      ""ProjectChanges"": {
                                          ""ConfigurationGeneral"": {
                                              ""Difference"": {
                                                  ""ChangedProperties"": [ ";

            if (_lastIntermediateOutputPath != _intermediateOutputPath)
            {
                ruleUpdate += @"                        ""IntermediateOutputPath"",";
            }
            // root namespace and project folder have changed if its the first time we've sent inputs
            if (_lastIntermediateOutputPath == null)
            {
                ruleUpdate += @"                        ""ProjectDir"",
                                                        ""RootNamespace""";
            }
            ruleUpdate = ruleUpdate.TrimEnd(',');
            ruleUpdate += @"                      ]
                                              },
                                              ""After"": {
                                                  ""Properties"": {
                                                      ""ProjectDir"": """ + _projectFolder.Replace("\\", "\\\\") + @""",
                                                      ""IntermediateOutputPath"": """ + _intermediateOutputPath.Replace("\\", "\\\\") + @""",
                                                      ""RootNamespace"": ""MyNamespace""
                                                  }
                                              }
                                          }
                                      }
                                  }";
            IProjectSubscriptionUpdate subscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(ruleUpdate);

            _lastIntermediateOutputPath = _intermediateOutputPath;

            // Ensure our input files are in the mock file system
            foreach (string file in inputs.Inputs.Concat(inputs.SharedInputs))
            {
                var fullFilePath = Path.Combine(_projectFolder, file);
                if (!_fileSystem.FileExists(fullFilePath))
                {
                    _fileSystem.AddFile(fullFilePath);
                }
            }

            _manager.ProcessDataflowChanges(new ProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>>(new Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>(inputs, subscriptionUpdate), ImmutableDictionary<NamedIdentity, IComparable>.Empty));
        }

        private async Task SendFileChange(params string[] files)
        {
            // Short delay so that files are actually newer than any previous output, since tests run fast
            await Task.Delay(1);

            // Ensure our input files are in, and up to date, in the mock file system
            foreach (string file in files)
            {
                var fullFilePath = Path.Combine(_projectFolder, file);
                _fileSystem.AddFile(fullFilePath);
            }

            _manager.ProcessFileChangeNotification(new ProjectVersionedValue<string[]>(files, ImmutableDictionary<NamedIdentity, IComparable>.Empty));
        }

        public void Dispose()
        {
            _manager.Dispose();
        }
    }
}
