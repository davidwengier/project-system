﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal partial class SDKVersionTelemetryServiceComponent
    {
        internal class SDKVersionTelemetryServiceInstance : OnceInitialisedOnceDisposedAsync, IMultiLifetimeInstance
        {
            private readonly IUnconfiguredProjectVsServices _projectVsServices;
            private readonly ISafeProjectGuidService _projectGuidService;
            private readonly ITelemetryService _telemetryService;
            private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;

            [ImportingConstructor]
            public SDKVersionTelemetryServiceInstance(
                IUnconfiguredProjectVsServices projectVsServices,
                ISafeProjectGuidService projectGuidService,
                ITelemetryService telemetryService,
                IUnconfiguredProjectTasksService unconfiguredProjectTasksService)
                : base(projectVsServices.ThreadingService.JoinableTaskContext)
            {
                _projectVsServices = projectVsServices;
                _projectGuidService = projectGuidService;
                _telemetryService = telemetryService;
                _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
            }

            protected override Task InitialiseCoreAsync(CancellationToken cancellationToken)
            {
                // Do not block initialization on reporting the sdk version. It is possible to deadlock.
                _projectVsServices.ThreadingService.RunAndForget(async () =>
                {
                    // Wait for the project to be loaded so that we don't prematurely load the active configuration
                    await _unconfiguredProjectTasksService.ProjectLoadedInHost;

                    ConfigurationGeneral projectProperties = await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();
                    Task<object> task = projectProperties?.NETCoreSdkVersion?.GetValueAsync();
                    string version = task == null ? string.Empty : (string)await task;
                    string projectId = await GetProjectIdAsync();

                    if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(projectId))
                    {
                        return;
                    }

                    _telemetryService.PostProperties(TelemetryEventName.SDKVersion, new[]
                    {
                        (TelemetryPropertyName.SDKVersionProject, (object)projectId),
                        (TelemetryPropertyName.SDKVersionNETCoreSdkVersion, version)
                    });
                }, 
                unconfiguredProject: _projectVsServices.Project);

                return Task.CompletedTask;
            }

            protected override Task DisposeCoreAsync(bool Initialised) => Task.CompletedTask;

            private async Task<string> GetProjectIdAsync()
            {
                Guid projectGuid = await _projectGuidService.GetProjectGuidAsync();
                return projectGuid == Guid.Empty ? null : projectGuid.ToString();
            }

            public Task InitialiseAsync()
            {
                return InitialiseAsync(CancellationToken.None);
            }
        }
    }
}
