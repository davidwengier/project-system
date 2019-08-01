// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

using InputTuple = System.Tuple<Microsoft.VisualStudio.ProjectSystem.VS.TempPE.DesignTimeInputs, string[]>;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal partial class TempPEBuildManager : UnconfiguredProjectHostBridge<IProjectVersionedValue<InputTuple>, IProjectVersionedValue<InputTuple>, IProjectVersionedValue<DesignTimeInputs>>, ITempPEBuildManager
    {
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly IActiveWorkspaceProjectContextHost _activeWorkspaceProjectContextHost;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IDesignTimeInputsDataSource _designTimeInputsDataSource;
        private readonly IDesignTimeInputsFileWatcher _designTimeInputsFileWatcher;

        // This field is protected to enable easier unit testing
        protected VSBuildManager BuildManager;

        [ImportingConstructor]
        public TempPEBuildManager(IProjectThreadingService threadingService,
                                  IUnconfiguredProjectCommonServices unconfiguredProjectServices,
                                  IActiveWorkspaceProjectContextHost activeWorkspaceProjectContextHost,
                                  IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
                                  IDesignTimeInputsDataSource designTimeInputsDataSource,
                                  IDesignTimeInputsFileWatcher designTimeInputsFileWatcher,
                                  VSBuildManager buildManager)
             : base(threadingService.JoinableTaskContext)
        {
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _activeWorkspaceProjectContextHost = activeWorkspaceProjectContextHost;
            _projectSubscriptionService = projectSubscriptionService;
            _designTimeInputsDataSource = designTimeInputsDataSource;
            _designTimeInputsFileWatcher = designTimeInputsFileWatcher;
            BuildManager = buildManager;
        }

        public string[] GetTempPEMonikers()
        {
            Initialize();

            return AppliedValue.Value.Inputs.ToArray();
        }

        public Task<string> GetTempPEDescriptionXmlAsync(string moniker)
        {
            return Task.FromResult("");
        }

        /// <summary>
        /// Called privately to actually fire the TempPE events and optionally recompile the TempPE library for the specified input
        /// </summary>
        private async Task FireTempPEDirtyAsync(string moniker, bool shouldCompile)
        {
            // Not using use the ThreadingService property because unit tests
            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            BuildManager.OnDesignTimeOutputDirty(moniker);
        }

        /// <summary>
        /// ApplyAsync is called on the UI thread and its job is to update AppliedValue to be correct based on the changes that have come through data flow after being processed
        /// </summary>
        protected override async Task ApplyAsync(IProjectVersionedValue<InputTuple> value)
        {
            // Not using use the ThreadingService property because unit tests
            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            // Apply our new value
            AppliedValue = new ProjectVersionedValue<DesignTimeInputs>(value.Value.Item1, value.DataSourceVersions);

            // Project properties changes cause all PEs to be dirty and recompile if this isn't the first update
            if (value.HasProjectPropertyChanges)
            {
                foreach (string item in newDesignTimeInputs)
                {
                    await FireTempPEDirtyAsync(item, value.ShouldCompile);
                }
            }
            else
            {
                // Individual inputs dirty their PEs and possibly recompile
                foreach (string item in addedDesignTimeInputs)
                {
                    await FireTempPEDirtyAsync(item, value.ShouldCompile);
                }
            }

            // Shared items cause all TempPEs to be dirty, but don't recompile, to match legacy behaviour
            if (hasRemovedDesignTimeSharedInputs || hasAddedDesignTimeSharedInputs)
            {
                // adding or removing shared design time inputs dirties things but doesn't recompile
                foreach (string item in newDesignTimeInputs)
                {
                    // We don't want to fire again if we already fired above and compiled
                    if (!addedDesignTimeInputs.Contains(item))
                    {
                        await FireTempPEDirtyAsync(item, false);
                    }
                }
            }

            foreach (string item in removedDesignTimeInputs)
            {
                BuildManager.OnDesignTimeOutputDeleted(item);
            }

            static bool TryGetValueIfUnused<T>(string item, ImmutableDictionary<string, T>.Builder source, ImmutableHashSet<string>.Builder otherSources, out T result)
            {
                // return the value from the source, but only if it doesn't appear in other sources
                return source.TryGetValue(item, out result) && !otherSources.Contains(item);
            }

        }

        /// <summary>
        /// InitializeInnerCoreAsync is responsible for setting an initial AppliedValue. This value will be used by any UI thread calls that may happen
        /// before the first data flow blocks have been processed. If this method doesn't set a value then the system will block until the first blocks
        /// have been applied.
        /// </summary>
        protected override Task InitializeInnerCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method is where we tell data flow which blocks we're interested in receiving updates for
        /// </summary>
        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<InputTuple>> targetBlock)
        {
            JoinUpstreamDataSources(_designTimeInputsDataSource, _designTimeInputsFileWatcher);

            return ProjectDataSources.SyncLinkTo(
                _designTimeInputsDataSource.SourceBlock.SyncLinkOptions(
                    linkOptions: DataflowOption.PropagateCompletion),
                _designTimeInputsFileWatcher.SourceBlock.SyncLinkOptions(
                    linkOptions: DataflowOption.PropagateCompletion),

                targetBlock,
                DataflowOption.PropagateCompletion,
                cancellationToken: ProjectAsynchronousTasksService.UnloadCancellationToken);
        }

        /// <summary>
        /// Preprocess gets called as each data flow block updates and its job is to take the input from those blocks and do whatever work needed
        /// so that ApplyAsync has all of the info it needs to do its job.
        /// </summary>
        protected override Task<IProjectVersionedValue<InputTuple>> PreprocessAsync(IProjectVersionedValue<InputTuple> input, IProjectVersionedValue<InputTuple> previousOutput)
        {
            DesignTimeInputs designTimeInputs = input.Value.Item1;
            string[] changedFiles = input.Value.Item2;

            var result = new ProjectVersionedValue<InputTuple>(new InputTuple(designTimeInputs, changedFiles), input.DataSourceVersions);

            return Task.FromResult((IProjectVersionedValue<InputTuple>)result);
        }
    }
}
