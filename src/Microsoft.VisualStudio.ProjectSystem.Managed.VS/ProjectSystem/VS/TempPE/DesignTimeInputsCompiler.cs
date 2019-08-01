// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputsCompiler : OnceInitializedOnceDisposed
    {
        private static readonly TimeSpan s_compilationDelayTime = TimeSpan.FromMilliseconds(500);


        private readonly IDesignTimeInputsChangeTracker _changeTracker;
        private readonly ITempPECompiler _compiler;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;
        private readonly TaskDelayScheduler _scheduler;

        private ImmutableArray<string> _designTimeInputs;
        private ImmutableArray<string> _sharedDesignTimeInputs;
        private ImmutableDictionary<string, bool> _filesToCompile = ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparers.Paths); // Key is filename, value is whether to ignore the last write time check
        private string? _outputPath;

        [ImportingConstructor]
        public DesignTimeInputsChangeTracker(UnconfiguredProject project,
                                     IDesignTimeInputsChangeTracker changeTracker,
                                     ITempPECompiler compiler,
                                     IFileSystem fileSystem,
                                     ITelemetryService telemetryService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _changeTracker = changeTracker;
            _compiler = compiler;
            _fileSystem = fileSystem;
            _telemetryService = telemetryService;
            _scheduler = new TaskDelayScheduler(s_compilationDelayTime, threadingService, CancellationToken.None);
        }

        /// <summary>
        /// This is to allow unit tests to run the compilation synchronously rather than waiting for async work to complete
        /// </summary>
        internal bool CompileSynchronously { get; set; }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (_inputsActionBlock != null)
            {
                // This will stop our blocks taking any more input
                _inputsActionBlock.Complete();
                _fileWatcherActionBlock!.Complete();

                await Task.WhenAll(_inputsActionBlock.Completion, _fileWatcherActionBlock.Completion);
            }

            // By waiting for completion we know that the following dispose will cancel any pending compilations, and there won't be any more
            _scheduler.Dispose();
            _disposables.Dispose();
        }

        private void QueueCompilation()
        {
            if (_filesToCompile.Count > 0)
            {
                JoinableTask task = _scheduler.ScheduleAsyncTask(ProcessCompileQueueAsync, _project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);
                if (CompileSynchronously)
                {
                    _threadingService.ExecuteSynchronously(() => task.Task);
                }
            }
        }

        private Task ProcessCompileQueueAsync(CancellationToken token)
        {
            int compileCount = 0;
            int initialQueueLength = _filesToCompile.Count;
            var compileStopWatch = Stopwatch.StartNew();
            return _activeWorkspaceProjectContextHost.OpenContextForWriteAsync(async accessor =>
            {
                // Grab the next file to compile off the queue
                (string fileName, bool ignoreFileWriteTime) = _filesToCompile.FirstOrDefault();
                while (fileName != null)
                {
                    if (IsDisposing || IsDisposed)
                    {
                        return;
                    }

                    if (token.IsCancellationRequested)
                    {
                        LogTelemetry(true);
                        return;
                    }

                    // Remove the file from our todo list. If it wasn't there (because it was removed as a design time input while we were busy) we don't need to compile it
                    bool wasInQueue = ThreadingTools.ApplyChangeOptimistically(ref _filesToCompile, fileName, (s, f) => s.Remove(f));

                    string outputFileName = await GetOutputFileNameAsync(fileName);
                    if (wasInQueue)
                    {
                        try
                        {
                            if (await CompileDesignTimeInputAsync(accessor.Context, fileName, outputFileName, ignoreFileWriteTime, token))
                            {
                                compileCount++;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            LogTelemetry(true);
                            return;
                        }
                    }

                    // Grab another file off the queue
                    (fileName, ignoreFileWriteTime) = _filesToCompile.FirstOrDefault();
                }

                LogTelemetry(false);
            });

            void LogTelemetry(bool cancelled)
            {
                compileStopWatch.Stop();
                _telemetryService.PostProperties(TelemetryEventName.TempPEProcessQueue, new[]
                {
                    ( TelemetryPropertyName.TempPECompileCount,        (object)compileCount),
                    ( TelemetryPropertyName.TempPEInitialQueueLength,  initialQueueLength),
                    ( TelemetryPropertyName.TempPECompileWasCancelled, cancelled),
                    ( TelemetryPropertyName.TempPECompileDuration,     compileStopWatch.ElapsedMilliseconds)
                });
            }
        }

        /// <summary>
        /// Gets the XML that describes a TempPE DLL, including building it if necessary
        /// </summary>
        /// <param name="fileName">A project relative path to a source file that is a design time input</param>
        /// <returns>An XML description of the TempPE DLL for the specified file</returns>
        public async Task<string> GetDesignTimeInputXmlAsync(string fileName)
        {
            // Make sure we're not being asked to compile a random file
            if (!_designTimeInputs.Contains(fileName, StringComparers.Paths))
            {
                throw new ArgumentException("Can only get XML snippets for design time inputs", nameof(fileName));
            }

            // Remove the file from our todo list, in case it was in there.
            ThreadingTools.ApplyChangeOptimistically(ref _filesToCompile, fileName, (s, f) => s.Remove(f));

            string outputFileName = await GetOutputFileNameAsync(fileName);
            // make sure the file is up to date
            bool compiled = await _activeWorkspaceProjectContextHost.OpenContextForWriteAsync(accessor =>
            {
                return CompileDesignTimeInputAsync(accessor.Context, fileName, outputFileName, ignoreFileWriteTime: false);
            });

            if (compiled)
            {
                _telemetryService.PostEvent(TelemetryEventName.TempPECompileOnDemand);
            }

            return $@"<root>
  <Application private_binpath = ""{Path.GetDirectoryName(outputFileName)}""/>
  <Assembly
    codebase = ""{Path.GetFileName(outputFileName)}""
    name = ""{fileName}""
    version = ""0.0.0.0""
    snapshot_id = ""1""
    replaceable = ""True""
  />
</root>";
        }

        private async Task<bool> CompileDesignTimeInputAsync(IWorkspaceProjectContext context, string designTimeInput, string outputFileName, bool ignoreFileWriteTime, CancellationToken token = default)
        {
            HashSet<string> filesToCompile = GetFilesToCompile(designTimeInput);

            if (ignoreFileWriteTime || CompilationNeeded(filesToCompile, outputFileName))
            {
                bool result = false;
                try
                {
                    result = await _compiler.CompileAsync(context, outputFileName, filesToCompile, token);
                }
                catch (IOException)
                { }
                finally
                {
                    // If the compilation failed or was cancelled we should clean up any old TempPE outputs lest a designer gets the wrong types, plus its what legacy did
                    // plus the way the Roslyn compiler works is by creating a 0 byte file first
                    if (!result)
                    {
                        try
                        {
                            _fileSystem.RemoveFile(outputFileName);
                        }
                        catch (IOException)
                        { }
                        catch (UnauthorizedAccessException)
                        { }
                    }
                }

                // true in this case means "we tried to compile", and is just for telemetry reasons. It doesn't indicate success or failure of compilation
                return true;
            }
            return false;
        }

        private async Task<string> GetOutputFileNameAsync(string designTimeInput)
        {
            // Wait until we've received at least one project rule update, or we won't know where to put the file
            await _receivedProjectRuleSource.Task;

            // All monikers are project relative paths by defintion (anything else is a link, and linked files can't be TempPE inputs), meaning 
            // the only invalid filename characters possible are path separators so we just replace them
            return Path.Combine(_outputPath, designTimeInput.Replace('\\', '.') + ".dll");
        }

        private bool CompilationNeeded(HashSet<string> files, string outputFileName)
        {
            if (!_fileSystem.FileExists(outputFileName))
            {
                return true;
            }

            try
            {
                DateTime outputDateTime = _fileSystem.LastFileWriteTimeUtc(outputFileName);

                foreach (string file in files)
                {
                    DateTime fileDateTime = _fileSystem.LastFileWriteTimeUtc(file);
                    if (fileDateTime > outputDateTime)
                        return true;
                }
            }
            // if we can't read the file time of the output file, then we presumably can't compile to it either, so returning false is appropriate.
            // if we can't read the file time of an input file, then we presumably can't read from it to compile either, so returning false is appropriate
            catch (IOException)
            { }
            catch (UnauthorizedAccessException)
            { }

            return false;
        }

        private HashSet<string> GetFilesToCompile(string moniker)
        {
            // This is a HashSet because we allow files to be both inputs and shared inputs, and we don't want to compile the same file twice,
            // plus Roslyn needs to call Contains on this quite a lot in order to ensure its only compiling the right files so we want that to be fast.
            // When it comes to compiling the files there is no difference between shared and normal design time inputs, we just track differently because
            // shared are included in every DLL.
            var files = new HashSet<string>(_sharedDesignTimeInputs.Length + 1, StringComparers.Paths);
            // All monikers are project relative paths by defintion (anything else is a link, and linked files can't be TempPE inputs) so we can convert
            // them to full paths using MakeRooted.
            files.AddRange(_sharedDesignTimeInputs.Select(_project.MakeRooted));
            files.Add(_project.MakeRooted(moniker));
            return files;
        }
    }
}
