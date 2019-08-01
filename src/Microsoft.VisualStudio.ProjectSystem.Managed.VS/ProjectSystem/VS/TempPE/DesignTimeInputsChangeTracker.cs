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
using System.Windows.Documents;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputsChangeTracker : ChainedProjectValueDataSourceBase<DesignTimeInputsDelta>, IDesignTimeInputsChangeTracker
    {
        private readonly UnconfiguredProject _project;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IActiveWorkspaceProjectContextHost _activeWorkspaceProjectContextHost;
        private readonly IProjectThreadingService _threadingService;
        private readonly IDesignTimeInputsDataSource _inputsDataSource;
        private readonly IDesignTimeInputsFileWatcher _fileWatcher;

        private readonly DisposableBag _disposables = new DisposableBag();

        private readonly TaskCompletionSource<bool> _receivedProjectRuleSource = new TaskCompletionSource<bool>();

        private ITargetBlock<IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>>>? _inputsActionBlock;
        private ITargetBlock<IProjectVersionedValue<string[]>>? _fileWatcherActionBlock;

        private DesignTimeInputs _latestDesignTimeInputs;
        private string _latestOutputPath;

        [ImportingConstructor]
        public DesignTimeInputsChangeTracker(UnconfiguredProject project,
                                     IProjectThreadingService threadingService,
                                     IDesignTimeInputsDataSource inputsDataSource,
                                     IDesignTimeInputsFileWatcher fileWatcher)
            : base()
        {
            _project = project;
            _threadingService = threadingService;
            _inputsDataSource = inputsDataSource;
            _fileWatcher = fileWatcher;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<DesignTimeInputsDelta>> targetBlock)
        {
            // Create an action block process the design time inputs and configuration general changes
            _inputsActionBlock = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>>>(ProcessDataflowChanges);

            IDisposable projectLink = ProjectDataSources.SyncLinkTo(
                   _inputsDataSource.SourceBlock.SyncLinkOptions(
                       linkOptions: DataflowOption.PropagateCompletion),
                   _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(
                      linkOptions: DataflowOption.WithRuleNames(ConfigurationGeneral.SchemaName)),
                   _inputsActionBlock,
                   DataflowOption.PropagateCompletion,
                   cancellationToken: _project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);

            // Create an action block to process file change notifications
            _fileWatcherActionBlock = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<string[]>>(ProcessFileChangeNotification);
            IDisposable watcherLink = _fileWatcher.SourceBlock.LinkTo(_fileWatcherActionBlock, DataflowOption.PropagateCompletion);

            _disposables.AddDisposable(projectLink);
            _disposables.AddDisposable(watcherLink);

            return Task.CompletedTask;
        }

        internal void ProcessFileChangeNotification(IProjectVersionedValue<string[]> arg)
        {
            // Ignore any file changes until we've received the first set of design time inputs (which shouldn't happen anyway)
            // That first update will queue all of the files so we're not losing anything
            if (_latestDesignTimeInputs == null)
            {
                return;
            }

            var changedFiles = new List<DesignTimeInputFileChange>();
            foreach (string changedFile in arg.Value)
            {
                string relativeFilePath = _project.MakeRelative(changedFile);

                // if a shared input changes, we recompile everything
                if (_latestDesignTimeInputs.SharedInputs.Contains(relativeFilePath))
                {
                    foreach (string file in _latestDesignTimeInputs.Inputs)
                    {
                        changedFiles.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime: false));
                    }
                    // Since we've just queued every file, we don't care about any other changes
                    break;
                }
                else
                {
                    changedFiles.Add(new DesignTimeInputFileChange(relativeFilePath, ignoreFileWriteTime: false));
                }
            }

            PostToOutput(changedFiles);
        }

        internal void ProcessDataflowChanges(IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>> input)
        {
            DesignTimeInputs inputs = input.Value.Item1;

            IProjectChangeDescription configChanges = input.Value.Item2.ProjectChanges[ConfigurationGeneral.SchemaName];

            var changedFiles = new List<DesignTimeInputFileChange>();
            // On the first call where we receive design time inputs we queue compilation of all of them, knowing that we'll only compile if the file write date requires it
            if (_latestDesignTimeInputs == null)
            {
                AddAllInputsToQueue(false);
            }
            else
            {
                // If its not the first call...

                // If a new shared design time input is added, we need to recompile everything regardless of source file modified date
                // because it could be an old file that is being promoted to a shared input
                if (inputs.SharedInputs.Except(_latestDesignTimeInputs.SharedInputs, StringComparers.Paths).Any())
                {
                    AddAllInputsToQueue(true);
                }
                // If the namespace or output path inputs have changed, then we recompile every file regardless of date
                else if (configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.RootNamespaceProperty) ||
                         configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.ProjectDirProperty) ||
                         configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.IntermediateOutputPathProperty))
                {
                    AddAllInputsToQueue(true);
                }
                else
                {
                    // Otherwise we just queue any new design time inputs, and still do date checks
                    foreach (string file in inputs.Inputs.Except(_latestDesignTimeInputs.Inputs, StringComparers.Paths))
                    {
                        changedFiles.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime: false));
                    }
                }
            }

            // Make sure we have the up to date list of inputs
            _latestDesignTimeInputs = inputs;

            // Make sure we have the up to date output path
            string basePath = configChanges.After.Properties[ConfigurationGeneral.ProjectDirProperty];
            string objPath = configChanges.After.Properties[ConfigurationGeneral.IntermediateOutputPathProperty];
            _latestOutputPath = GetOutputPath(basePath, objPath);

            _receivedProjectRuleSource.TrySetResult(true);

            PostToOutput(changedFiles);

            void AddAllInputsToQueue(bool ignoreFileWriteTime)
            {
                foreach (string file in inputs.Inputs)
                {
                    changedFiles.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime));
                }
            }
        }

        internal static string GetOutputPath(string projectPath, string intermediateOutputPath)
        {
            return Path.Combine(projectPath, intermediateOutputPath, "TempPE");
        }

        private void PostToOutput(List<DesignTimeInputFileChange> changedFiles)
        {
            var delta = new DesignTimeInputsDelta(_latestDesignTimeInputs.Inputs, _latestDesignTimeInputs.SharedInputs, changedFiles, _latestOutputPath);
        }

    }
}
