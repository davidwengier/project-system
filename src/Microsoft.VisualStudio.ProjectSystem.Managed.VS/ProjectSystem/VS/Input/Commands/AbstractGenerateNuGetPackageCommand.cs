// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractGenerateNuGetPackageCommand : AbstractSingleNodeProjectCommand, IDisposable
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsService<IVsSolutionBuildManager2> _vsSolutionBuildManagerService;
        private readonly GeneratePackageOnBuildPropertyProvider _generatePackageOnBuildPropertyProvider;
        private IVsSolutionBuildManager2? _buildManager;

        protected AbstractGenerateNuGetPackageCommand(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> vsSolutionBuildManagerService,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(vsSolutionBuildManagerService, nameof(vsSolutionBuildManagerService));
            Requires.NotNull(generatePackageOnBuildPropertyProvider, nameof(generatePackageOnBuildPropertyProvider));

            Project = project;
            _threadingService = threadingService;
            _vsSolutionBuildManagerService = vsSolutionBuildManagerService;
            _generatePackageOnBuildPropertyProvider = generatePackageOnBuildPropertyProvider;
        }

        protected UnconfiguredProject Project { get; }

        protected abstract string GetCommandText();

        protected abstract bool ShouldHandle(IProjectTree node);

        protected override async Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            if (ShouldHandle(node))
            {
                // Enable the command if the build manager is ready to build.
                CommandStatus commandStatus = await IsReadyToBuildAsync() ? CommandStatus.Enabled : CommandStatus.Supported;
                return await GetCommandStatusResult.Handled(GetCommandText(), commandStatus);
            }

            return CommandStatusResult.Unhandled;
        }

        private async Task<bool> IsReadyToBuildAsync()
        {
            // Ensure build manager is initialized.
            await EnsureBuildManagerInitializedAsync();

            ErrorHandler.ThrowOnFailure(_buildManager!.QueryBuildManagerBusy(out int busy));
            return busy == 0;
        }

        private async Task EnsureBuildManagerInitializedAsync()
        {
            // Switch to UI thread for querying the build manager service.
            await _threadingService.SwitchToUIThread();

            if (_buildManager == null)
            {
                _buildManager = await _vsSolutionBuildManagerService.GetValueAsync();
            }
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!ShouldHandle(node))
            {
                return false;
            }

            if (await IsReadyToBuildAsync())
            {
                // Build manager APIs require UI thread access.
                await _threadingService.SwitchToUIThread();

                // Save documents before build.
                var projectVsHierarchy = (IVsHierarchy)Project.Services.HostObject;
                ErrorHandler.ThrowOnFailure(_buildManager!.SaveDocumentsBeforeBuild(projectVsHierarchy, (uint)VSConstants.VSITEMID.Root, 0 /*docCookie*/));

                // Kick off the build.
                uint dwFlags = (uint)(VSSOLNBUILDUPDATEFLAGS.SBF_SUPPRESS_SAVEBEFOREBUILD_QUERY | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD);

                // Tell the Solution Build Manager that we want to package
                uint[] buildFlags = new[] { VSConstants.VS_BUILDABLEPROJECTCFGOPTS_PACKAGE };

                ErrorHandler.ThrowOnFailure(_buildManager.StartUpdateSpecificProjectConfigurations(1, new[] { projectVsHierarchy }, null, null, buildFlags, null, dwFlags, 0));
            }

            return true;
        }

        #region IDisposable
        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _buildManager != null)
                {
                    // Build manager APIs require UI thread access.
                    _threadingService.ExecuteSynchronously(async () =>
                    {
                        await _threadingService.SwitchToUIThread();

                        if (_buildManager != null)
                        {
                            _buildManager = null;
                        }
                    });
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
